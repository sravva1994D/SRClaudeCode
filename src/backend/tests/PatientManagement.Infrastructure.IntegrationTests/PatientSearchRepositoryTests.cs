using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Patients;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class PatientSearchRepositoryTests
{
    private const string InsertDoctorSql =
        "INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'dev.doctor', N'hash');";

    private static readonly DateOnly Today = new(2026, 11, 11);
    private static readonly DateOnly DateOfBirth = new(1990, 5, 14);

    [Fact]
    public async Task SearchActiveAsync_PartialNameAnyCase_ReturnsMatchingPatients()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, search) = Create(database);
        await AddAsync(unitOfWork, patients, "Asha Rao", "9876543210");
        await AddAsync(unitOfWork, patients, "Vikram Shah", "9123456780");

        var page = await search.SearchActiveAsync("ASH", null, 1, 20, CancellationToken.None);

        page.TotalCount.Should().Be(1);
        page.Items.Should().ContainSingle().Which.FullName.Should().Be("Asha Rao");
    }

    [Fact]
    public async Task SearchActiveAsync_PhoneDigits_ReturnsPatientsWithThatNumber()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, search) = Create(database);
        await AddAsync(unitOfWork, patients, "Asha Rao", "9876543210");
        await AddAsync(unitOfWork, patients, "Vikram Shah", "9123456780");

        var page = await search.SearchActiveAsync(null, "98765", 1, 20, CancellationToken.None);

        page.Items.Should().ContainSingle().Which.PhoneNumber.Should().Be("9876543210");
    }

    [Fact]
    public async Task SearchActiveAsync_DeactivatedPatient_IsNotListed()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, search) = Create(database);
        var patient = await AddAsync(unitOfWork, patients, "Asha Rao", null);
        var auditValuesBefore = patient.AuditValues();
        patient.Deactivate();
        await unitOfWork.ExecuteAsync(
            token => patients.SaveAsync(patient, auditValuesBefore, AuditAction.Delete, 1, token),
            CancellationToken.None);

        var page = await search.SearchActiveAsync(null, null, 1, 20, CancellationToken.None);

        page.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchActiveAsync_SecondPage_ReturnsTheRestWithTheFullTotal()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, search) = Create(database);
        await AddAsync(unitOfWork, patients, "Asha Rao", null);
        await AddAsync(unitOfWork, patients, "Bina Das", null);
        await AddAsync(unitOfWork, patients, "Chetan Iyer", null);

        var page = await search.SearchActiveAsync(null, null, 2, 2, CancellationToken.None);

        page.TotalCount.Should().Be(3);
        page.Items.Should().ContainSingle().Which.FullName.Should().Be("Chetan Iyer");
    }

    [Fact]
    public async Task SearchActiveAsync_PercentSignInName_MatchesLiterally()
    {
        await using var database = await TestDatabase.CreateAsync();
        await SetUpAsync(database);
        var (unitOfWork, patients, search) = Create(database);
        await AddAsync(unitOfWork, patients, "Asha Rao", null);

        var page = await search.SearchActiveAsync("%", null, 1, 20, CancellationToken.None);

        page.TotalCount.Should().Be(0);
    }

    private static async Task SetUpAsync(TestDatabase database)
    {
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertDoctorSql);
    }

    private static async Task<Patient> AddAsync(
        IUnitOfWork unitOfWork,
        IPatientRepository patients,
        string fullName,
        string? phoneNumber)
    {
        var patient = Patient.Register(fullName, DateOfBirth, 2, phoneNumber, null, Today);
        await unitOfWork.ExecuteAsync(token => patients.AddAsync(patient, 1, token), CancellationToken.None);
        return patient;
    }

    private static (IUnitOfWork UnitOfWork, IPatientRepository Patients, IPatientSearch Search) Create(TestDatabase database)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var clock = new SystemClock();
        var audit = new SqlAuditLogWriter(clock, options);
        var connectionFactory = new SqlConnectionFactory(options);
        return (
            new SqlUnitOfWork(connectionFactory),
            new SqlPatientRepository(audit, clock, connectionFactory, options),
            new SqlPatientSearch(connectionFactory, options));
    }
}
