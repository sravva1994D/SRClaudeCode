using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Consultation;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Reads and saves a visit's medications inside the current unit of work. Every change is audited in the same transaction.</summary>
public sealed class SqlMedicationRepository(
    IAuditLogWriter auditLogWriter,
    IClock clock,
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IMedicationRepository
{
    private const string TableName = "Medications";

    private const string ListActiveSql = """
        SELECT MedicationId, VisitId, MedicineName, Dosage, Frequency, Duration, SpecialInstructions, SortOrder, IsActive, RowVersion
        FROM dbo.Medications
        WHERE VisitId = @VisitId AND IsActive = 1
        ORDER BY SortOrder, MedicationId;
        """;

    private const string InsertSql = """
        INSERT INTO dbo.Medications (VisitId, MedicineName, Dosage, Frequency, Duration, SpecialInstructions, SortOrder, CreatedBy, ModifiedDate, ModifiedBy)
        OUTPUT inserted.MedicationId, inserted.RowVersion
        VALUES (@VisitId, @MedicineName, @Dosage, @Frequency, @Duration, @Instructions, @SortOrder, @ChangedBy, @ModifiedDate, @ChangedBy);
        """;

    // Only an active row can be deactivated. Zero rows affected means the row changed under the caller.
    private const string DeactivateSql = """
        UPDATE dbo.Medications
        SET IsActive = 0, ModifiedDate = @ModifiedDate, ModifiedBy = @ChangedBy
        WHERE MedicationId = @MedicationId AND IsActive = 1;
        """;

    public async Task<IReadOnlyList<Medication>> ListActiveForVisitAsync(int visitId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.TryGetCurrent();
        if (scope is not null)
        {
            return await ReadAsync(scope.Connection, scope.Transaction, visitId, cancellationToken);
        }

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await ReadAsync(connection, transaction: null, visitId, cancellationToken);
    }

    public async Task AddAsync(Medication medication, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = scope.Connection.CreateCommand();
        command.CommandText = InsertSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@VisitId", medication.VisitId));
        command.Parameters.Add(new SqlParameter("@MedicineName", medication.MedicineName));
        command.Parameters.Add(new SqlParameter("@Dosage", medication.Dosage));
        command.Parameters.Add(new SqlParameter("@Frequency", medication.Frequency));
        command.Parameters.Add(new SqlParameter("@Duration", medication.Duration));
        command.Parameters.Add(new SqlParameter("@Instructions", (object?)medication.SpecialInstructions ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@SortOrder", medication.SortOrder));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("The medication insert returned no row.");
        }

        medication.AssignMedicationId(reader.GetInt32(0));
        medication.SetRowVersion((byte[])reader.GetValue(1));
        await reader.CloseAsync();

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                medication.MedicationId.ToString(CultureInfo.InvariantCulture),
                AuditAction.Create,
                OldValues: null,
                NewValues: medication.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    public async Task DeactivateAsync(Medication medication, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        var before = medication.AuditValues();
        medication.Deactivate();

        await using var command = scope.Connection.CreateCommand();
        command.CommandText = DeactivateSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@MedicationId", medication.MedicationId));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));

        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new InvalidOperationException("The medication to deactivate was not active.");
        }

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                medication.MedicationId.ToString(CultureInfo.InvariantCulture),
                AuditAction.Update,
                OldValues: before,
                NewValues: medication.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    private static async Task<IReadOnlyList<Medication>> ReadAsync(
        DbConnection connection,
        DbTransaction? transaction,
        int visitId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = ListActiveSql;
        command.Transaction = transaction;
        command.Parameters.Add(new SqlParameter("@VisitId", visitId));

        var medications = new List<Medication>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            medications.Add(Medication.Reconstitute(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetInt32(7),
                reader.GetBoolean(8),
                (byte[])reader.GetValue(9)));
        }

        return medications;
    }
}

/// <summary>
/// Finds medicine names from the doctor's own history. The prefix is matched with LIKE on the MedicineName index, so the
/// LIKE wildcards in the search text are escaped and the search is a prefix match only.
/// </summary>
public sealed class SqlMedicineSuggestionRepository(
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IMedicineSuggestionRepository
{
    private const string SuggestSql = """
        SELECT DISTINCT TOP (@Limit) MedicineName
        FROM dbo.Medications
        WHERE IsActive = 1 AND MedicineName LIKE @Pattern ESCAPE '\'
        ORDER BY MedicineName;
        """;

    public async Task<IReadOnlyList<string>> FindNamesStartingWithAsync(
        string prefix,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SuggestSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@Limit", limit));
        command.Parameters.Add(new SqlParameter("@Pattern", EscapeLike(prefix) + "%"));

        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    /// <summary>Escapes the characters LIKE treats as wildcards, so a search for "5%" matches the text "5%" only.</summary>
    private static string EscapeLike(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            if (character is '\\' or '%' or '_' or '[')
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
