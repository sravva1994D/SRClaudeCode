using System.Text.RegularExpressions;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;

namespace PatientManagement.Application.Patients.Handlers;

/// <summary>Reads one active patient.</summary>
public sealed class GetPatientQueryHandler(IPatientRepository patients)
{
    public async Task<PatientDetails> HandleAsync(int patientId, CancellationToken cancellationToken)
    {
        var patient = await patients.FindActiveAsync(patientId, cancellationToken)
            ?? throw new RecordNotFoundException("Patient");
        return PatientDetails.From(patient);
    }
}

/// <summary>Returns the active patients that match a name and date of birth. The duplicate warning shows at most five.</summary>
public sealed class FindDuplicatePatientsQueryHandler(IPatientRepository patients)
{
    public const int MaximumMatches = 5;

    public async Task<IReadOnlyList<PatientSummary>> HandleAsync(
        FindDuplicatePatientsQuery query,
        CancellationToken cancellationToken)
    {
        var matches = await patients.FindActiveDuplicatesAsync(
            query.FullName.Trim(),
            query.DateOfBirth,
            query.ExcludePatientId,
            MaximumMatches,
            cancellationToken);
        return matches.Select(PatientSummary.From).ToList();
    }
}

/// <summary>Registers a patient. The insert and its audit row commit together.</summary>
public sealed class CreatePatientCommandHandler(
    IUnitOfWork unitOfWork,
    IPatientRepository patients,
    IClock clock)
{
    public Task<PatientDetails> HandleAsync(CreatePatientCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var fields = PatientFieldNormalizer.Normalize(command.Fields);
            var patient = Patient.Register(
                fields.FullName,
                fields.DateOfBirth,
                fields.GenderId,
                fields.PhoneNumber,
                fields.Address,
                Today(clock));

            await patients.AddAsync(patient, command.ChangedByUserId, token);
            return PatientDetails.From(patient);
        }, cancellationToken);

    private static DateOnly Today(IClock clock) => DateOnly.FromDateTime(clock.UtcNow);
}

/// <summary>Changes a patient's details. The change and its audit row commit together.</summary>
public sealed class UpdatePatientCommandHandler(
    IUnitOfWork unitOfWork,
    IPatientRepository patients,
    IClock clock)
{
    public Task<PatientDetails> HandleAsync(UpdatePatientCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var patient = await patients.FindActiveAsync(command.PatientId, token)
                ?? throw new RecordNotFoundException("Patient");
            var auditValuesBefore = patient.AuditValues();
            var fields = PatientFieldNormalizer.Normalize(command.Fields);

            patient.Update(
                fields.FullName,
                fields.DateOfBirth,
                fields.GenderId,
                fields.PhoneNumber,
                fields.Address,
                DateOnly.FromDateTime(clock.UtcNow));

            await patients.SaveAsync(patient, auditValuesBefore, AuditAction.Update, command.ChangedByUserId, token);
            return PatientDetails.From(patient);
        }, cancellationToken);
}

/// <summary>Deactivates a patient. The row stays in the database; the audit row records the deactivation.</summary>
public sealed class DeletePatientCommandHandler(IUnitOfWork unitOfWork, IPatientRepository patients)
{
    public Task HandleAsync(DeletePatientCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var patient = await patients.FindActiveAsync(command.PatientId, token)
                ?? throw new RecordNotFoundException("Patient");
            var auditValuesBefore = patient.AuditValues();

            patient.Deactivate();

            await patients.SaveAsync(patient, auditValuesBefore, AuditAction.Delete, command.ChangedByUserId, token);
        }, cancellationToken);
}

/// <summary>Trims text, stores blank optional text as NULL, and keeps only digits in the phone number (CLAUDE.md 6.6).</summary>
public static class PatientFieldNormalizer
{
    private static readonly Regex NonDigits = new("[^0-9]", RegexOptions.CultureInvariant);

    public static PatientFields Normalize(PatientFields fields) => fields with
    {
        FullName = fields.FullName.Trim(),
        PhoneNumber = Digits(fields.PhoneNumber),
        Address = Blank(fields.Address),
    };

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Digits(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : NonDigits.Replace(value, string.Empty);
}
