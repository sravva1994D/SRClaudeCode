using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Prescriptions;

namespace PatientManagement.Application.Prescriptions.Handlers;

/// <summary>
/// Records a print of the issued prescription. The content is built from the visit's current records and stored with the
/// print, so later edits to the visit do not change what was printed. The print and its snapshot commit together.
/// </summary>
public sealed class RecordPrescriptionPrintCommandHandler(
    IUnitOfWork unitOfWork,
    IVisitRepository visits,
    IPrescriptionRepository prescriptions,
    IPrescriptionPrintRepository prints,
    GetPrescriptionQueryHandler getPrescription,
    IPrescriptionSnapshotCodec codec,
    IClock clock)
{
    public Task<PrescriptionPrintDetails> HandleAsync(
        RecordPrescriptionPrintCommand command,
        CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var visit = await visits.FindActiveAsync(command.VisitId, token)
                ?? throw new RecordNotFoundException("Visit");
            var prescription = await prescriptions.FindActiveForVisitAsync(visit.VisitId, token)
                ?? throw new RecordNotFoundException("Prescription");

            var document = await getPrescription.HandleAsync(new GetPrescriptionQuery(visit.VisitId), token);
            var printedAt = clock.UtcNow;
            var print = PrescriptionPrint.Record(prescription.PrescriptionId, printedAt, codec.Serialize(document));
            await prints.AddAsync(print, command.ChangedByUserId, token);

            return new PrescriptionPrintDetails(print.PrescriptionPrintId, printedAt, document);
        }, cancellationToken);
}

/// <summary>Lists the prints of the visit's prescription, oldest first, for the reprint list. Read-only.</summary>
public sealed class GetPrescriptionPrintsQueryHandler(
    IVisitRepository visits,
    IPrescriptionRepository prescriptions,
    IPrescriptionPrintRepository prints)
{
    public async Task<IReadOnlyList<PrescriptionPrintSummary>> HandleAsync(
        GetPrescriptionPrintsQuery query,
        CancellationToken cancellationToken)
    {
        var visit = await visits.FindActiveAsync(query.VisitId, cancellationToken)
            ?? throw new RecordNotFoundException("Visit");
        var prescription = await prescriptions.FindActiveForVisitAsync(visit.VisitId, cancellationToken)
            ?? throw new RecordNotFoundException("Prescription");

        var saved = await prints.ListForPrescriptionAsync(prescription.PrescriptionId, cancellationToken);
        return saved.Select(print => new PrescriptionPrintSummary(print.PrescriptionPrintId, print.PrintedAt)).ToList();
    }
}

/// <summary>Reads one print with the content that was printed, from its snapshot. Read-only.</summary>
public sealed class GetPrescriptionPrintQueryHandler(
    IVisitRepository visits,
    IPrescriptionRepository prescriptions,
    IPrescriptionPrintRepository prints,
    IPrescriptionSnapshotCodec codec)
{
    public async Task<PrescriptionPrintDetails> HandleAsync(
        GetPrescriptionPrintQuery query,
        CancellationToken cancellationToken)
    {
        var visit = await visits.FindActiveAsync(query.VisitId, cancellationToken)
            ?? throw new RecordNotFoundException("Visit");
        var prescription = await prescriptions.FindActiveForVisitAsync(visit.VisitId, cancellationToken)
            ?? throw new RecordNotFoundException("Prescription");
        var print = await prints.FindAsync(prescription.PrescriptionId, query.PrescriptionPrintId, cancellationToken)
            ?? throw new RecordNotFoundException("Prescription print");

        return new PrescriptionPrintDetails(
            print.PrescriptionPrintId,
            print.PrintedAt,
            codec.Deserialize(print.SnapshotJson));
    }
}
