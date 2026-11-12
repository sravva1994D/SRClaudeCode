using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Consultation;

namespace PatientManagement.Application.Consultation.Handlers;

/// <summary>
/// Records a visit's vitals. The visit must be in progress and must not already have vitals. An impossible value is
/// rejected; an abnormal value is saved and returned as a warning. The save and its audit row commit together.
/// </summary>
public sealed class RecordVitalsCommandHandler(
    IUnitOfWork unitOfWork,
    IVisitRepository visits,
    IVitalRangeRepository ranges,
    IVitalsRepository vitals)
{
    public Task<RecordVitalsResult> HandleAsync(RecordVitalsCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var visit = await visits.FindActiveAsync(command.VisitId, token)
                ?? throw new RecordNotFoundException("Visit");
            visit.EnsureInProgress();

            var configured = await ranges.GetActiveAsync(token);
            var recorded = Domain.Consultation.Vitals.Record(
                visit.VisitId,
                command.Temperature,
                command.BloodPressureSystolic,
                command.BloodPressureDiastolic,
                command.Pulse,
                configured,
                out var warnings);

            await vitals.AddAsync(recorded, command.ChangedByUserId, token);
            return new RecordVitalsResult(VitalsDetails.From(recorded), warnings);
        }, cancellationToken);
}

/// <summary>Reads the vitals recorded for a visit.</summary>
public sealed class GetVitalsQueryHandler(IVisitRepository visits, IVitalsRepository vitals)
{
    public async Task<VitalsDetails> HandleAsync(GetVitalsQuery query, CancellationToken cancellationToken)
    {
        _ = await visits.FindActiveAsync(query.VisitId, cancellationToken)
            ?? throw new RecordNotFoundException("Visit");
        var recorded = await vitals.FindActiveForVisitAsync(query.VisitId, cancellationToken)
            ?? throw new RecordNotFoundException("Vitals");
        return VitalsDetails.From(recorded);
    }
}
