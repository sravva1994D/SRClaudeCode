using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Prescriptions;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Reads and records prescription prints inside the current unit of work. Prints are not audited (Q17).</summary>
public sealed class SqlPrescriptionPrintRepository(
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IPrescriptionPrintRepository
{
    // The list reads no snapshot: a reprint list needs only the time of each print.
    private const string ListForPrescriptionSql = """
        SELECT PrescriptionPrintId, PrescriptionId, PrintedAt
        FROM dbo.PrescriptionPrints
        WHERE PrescriptionId = @PrescriptionId AND IsActive = 1
        ORDER BY PrintedAt, PrescriptionPrintId;
        """;

    private const string FindSql = """
        SELECT PrescriptionPrintId, PrescriptionId, PrintedAt, SnapshotJson
        FROM dbo.PrescriptionPrints
        WHERE PrescriptionPrintId = @PrescriptionPrintId AND PrescriptionId = @PrescriptionId AND IsActive = 1;
        """;

    private const string InsertSql = """
        INSERT INTO dbo.PrescriptionPrints (PrescriptionId, PrintedAt, SnapshotJson, CreatedBy, ModifiedDate, ModifiedBy)
        OUTPUT inserted.PrescriptionPrintId
        VALUES (@PrescriptionId, @PrintedAt, @SnapshotJson, @ChangedBy, @PrintedAt, @ChangedBy);
        """;

    public async Task<IReadOnlyList<PrescriptionPrint>> ListForPrescriptionAsync(
        int prescriptionId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = ListForPrescriptionSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@PrescriptionId", prescriptionId));

        var prints = new List<PrescriptionPrint>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            prints.Add(PrescriptionPrint.Reconstitute(
                reader.GetInt32(0),
                reader.GetInt32(1),
                DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc),
                snapshotJson: string.Empty));
        }

        return prints;
    }

    public async Task<PrescriptionPrint?> FindAsync(
        int prescriptionId,
        int prescriptionPrintId,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = FindSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@PrescriptionPrintId", prescriptionPrintId));
        command.Parameters.Add(new SqlParameter("@PrescriptionId", prescriptionId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return PrescriptionPrint.Reconstitute(
            reader.GetInt32(0),
            reader.GetInt32(1),
            DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc),
            reader.GetString(3));
    }

    public async Task AddAsync(PrescriptionPrint print, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = scope.Connection.CreateCommand();
        command.CommandText = InsertSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@PrescriptionId", print.PrescriptionId));
        command.Parameters.Add(new SqlParameter("@PrintedAt", print.PrintedAt));
        command.Parameters.Add(new SqlParameter("@SnapshotJson", print.SnapshotJson));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));

        var id = await command.ExecuteScalarAsync(cancellationToken);
        print.AssignPrescriptionPrintId(Convert.ToInt32(id, System.Globalization.CultureInfo.InvariantCulture));
    }
}
