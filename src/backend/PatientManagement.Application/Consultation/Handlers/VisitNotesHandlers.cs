using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Consultation;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Application.Consultation.Handlers;

/// <summary>
/// Replaces a visit's complaints with the draft. The visit row is locked and its status checked again inside the unit
/// of work, so a save cannot race a completed visit. An unchanged draft writes nothing.
/// </summary>
public sealed class SaveComplaintsCommandHandler(
    IUnitOfWork unitOfWork,
    IVisitRepository visits,
    IComplaintRepository complaints)
{
    public Task HandleAsync(SaveComplaintsCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var visit = await visits.FindActiveForUpdateAsync(command.VisitId, token)
                ?? throw new RecordNotFoundException("Visit");
            visit.EnsureInProgress();

            var wanted = (command.Complaints ?? Array.Empty<string>())
                .Select(text => (text ?? string.Empty).Trim())
                .Where(text => text.Length > 0)
                .ToList();

            var active = await complaints.ListActiveForVisitAsync(visit.VisitId, token);
            if (active.Select(complaint => complaint.Text).SequenceEqual(wanted, StringComparer.Ordinal))
            {
                return;
            }

            foreach (var replaced in active)
            {
                await complaints.DeactivateAsync(replaced, command.ChangedByUserId, token);
            }

            for (var index = 0; index < wanted.Count; index++)
            {
                var complaint = Complaint.Record(visit.VisitId, wanted[index], index);
                await complaints.AddAsync(complaint, command.ChangedByUserId, token);
            }
        }, cancellationToken);
}

/// <summary>
/// Replaces a visit's diagnoses with the draft. Same locking and unchanged-draft rules as complaints. The code is
/// compared with the text, so changing only a code is a change.
/// </summary>
public sealed class SaveDiagnosesCommandHandler(
    IUnitOfWork unitOfWork,
    IVisitRepository visits,
    IDiagnosisRepository diagnoses)
{
    public Task HandleAsync(SaveDiagnosesCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var visit = await visits.FindActiveForUpdateAsync(command.VisitId, token)
                ?? throw new RecordNotFoundException("Visit");
            visit.EnsureInProgress();

            var wanted = (command.Diagnoses ?? Array.Empty<DiagnosisEntry>())
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Text))
                .ToList();

            var active = await diagnoses.ListActiveForVisitAsync(visit.VisitId, token);
            var unchanged = active.Select(diagnosis => (diagnosis.Text, diagnosis.Code))
                .SequenceEqual(wanted.Select(entry => (Text: entry.Text!.Trim(), Code: TrimmedCode(entry.Code))));
            if (unchanged)
            {
                return;
            }

            foreach (var replaced in active)
            {
                await diagnoses.DeactivateAsync(replaced, command.ChangedByUserId, token);
            }

            for (var index = 0; index < wanted.Count; index++)
            {
                var diagnosis = Diagnosis.Record(visit.VisitId, wanted[index].Text!, wanted[index].Code, index);
                await diagnoses.AddAsync(diagnosis, command.ChangedByUserId, token);
            }
        }, cancellationToken);

    private static string? TrimmedCode(string? code)
    {
        var trimmed = code?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}

/// <summary>Reads a visit's saved complaints and diagnoses. Read-only: no transaction.</summary>
public sealed class GetVisitNotesQueryHandler(
    IVisitRepository visits,
    IComplaintRepository complaints,
    IDiagnosisRepository diagnoses)
{
    public async Task<VisitNotesDetails> HandleAsync(GetVisitNotesQuery query, CancellationToken cancellationToken)
    {
        _ = await visits.FindActiveAsync(query.VisitId, cancellationToken)
            ?? throw new RecordNotFoundException("Visit");

        var savedComplaints = await complaints.ListActiveForVisitAsync(query.VisitId, cancellationToken);
        var savedDiagnoses = await diagnoses.ListActiveForVisitAsync(query.VisitId, cancellationToken);

        return new VisitNotesDetails(
            query.VisitId,
            savedComplaints.Select(complaint => complaint.Text).ToList(),
            savedDiagnoses.Select(diagnosis => new DiagnosisDetails(diagnosis.Text, diagnosis.Code)).ToList());
    }
}
