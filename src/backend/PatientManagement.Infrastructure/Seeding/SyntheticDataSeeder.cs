using System.Data.Common;
using Microsoft.Data.SqlClient;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.Seeding;

/// <summary>
/// Writes development-only synthetic data: one doctor account and placeholder clinic text. Re-running it adds nothing
/// new and never overwrites clinic text that someone has already filled in. Never seed real data.
/// </summary>
public sealed class SyntheticDataSeeder(ISqlConnectionFactory connectionFactory)
{
    public const string DoctorUsername = "dev.doctor";

    private const int CommandTimeoutSeconds = 30;

    private const string InsertDoctorSql = """
        INSERT INTO dbo.Users (Username, PasswordHash)
        SELECT @Username, @PasswordHash
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username);
        """;

    // Fills only blank fields, so a second run cannot overwrite the doctor's own clinic details.
    private const string FillClinicProfileSql = """
        UPDATE dbo.ClinicProfile
        SET ClinicName = @ClinicName,
            DoctorName = @DoctorName,
            Qualifications = @Qualifications,
            RegistrationNumber = @RegistrationNumber,
            Address = @Address,
            PhoneNumber = @PhoneNumber,
            FooterNotes = @FooterNotes,
            ModifiedDate = SYSUTCDATETIME()
        WHERE ClinicProfileId = 1 AND ClinicName IS NULL;
        """;

    public async Task SeedAsync(string doctorPasswordHash, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ExecuteAsync(
            connection,
            transaction,
            InsertDoctorSql,
            [new SqlParameter("@Username", DoctorUsername), new SqlParameter("@PasswordHash", doctorPasswordHash)],
            cancellationToken);

        await ExecuteAsync(
            connection,
            transaction,
            FillClinicProfileSql,
            [
                new SqlParameter("@ClinicName", "Synthetic Clinic"),
                new SqlParameter("@DoctorName", "Dr. Test Physician"),
                new SqlParameter("@Qualifications", "MBBS (synthetic)"),
                new SqlParameter("@RegistrationNumber", "SYN-0001"),
                new SqlParameter("@Address", "1 Test Street, Sample City"),
                new SqlParameter("@PhoneNumber", "0000000000"),
                new SqlParameter("@FooterNotes", "Synthetic development data. Not a real prescription."),
            ],
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task ExecuteAsync(
        DbConnection connection,
        DbTransaction transaction,
        string sql,
        SqlParameter[] parameters,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Transaction = transaction;
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
