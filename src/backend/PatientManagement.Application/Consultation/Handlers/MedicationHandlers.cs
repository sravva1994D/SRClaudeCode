using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Consultation;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Application.Consultation.Handlers;

/// <summary>
/// Replaces a visit's medications with the draft. The visit row is locked so saves for one visit run in order, and medications
/// can be edited at any visit status (Q2). Each entry is checked by the domain (required fields), so an incomplete medicine
/// is refused before anything is written. An unchanged draft writes nothing; a change stamps the visit's last-modified time.
/// </summary>
public sealed class SaveMedicationsCommandHandler(
    IUnitOfWork unitOfWork,
    IVisitRepository visits,
    IMedicationRepository medications)
{
    public Task HandleAsync(SaveMedicationsCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var visit = await visits.FindActiveForUpdateAsync(command.VisitId, token)
                ?? throw new RecordNotFoundException("Visit");

            var wanted = (command.Medications ?? Array.Empty<MedicationEntry>())
                .Where(entry => !string.IsNullOrWhiteSpace(entry.MedicineName))
                .Select((entry, index) => Medication.Record(
                    visit.VisitId,
                    entry.MedicineName!,
                    entry.Dosage ?? string.Empty,
                    entry.Frequency ?? string.Empty,
                    entry.Duration ?? string.Empty,
                    entry.SpecialInstructions,
                    index))
                .ToList();

            var active = await medications.ListActiveForVisitAsync(visit.VisitId, token);
            var unchanged = active.Select(Key).SequenceEqual(wanted.Select(Key));
            if (unchanged)
            {
                return;
            }

            foreach (var replaced in active)
            {
                await medications.DeactivateAsync(replaced, command.ChangedByUserId, token);
            }

            foreach (var medication in wanted)
            {
                await medications.AddAsync(medication, command.ChangedByUserId, token);
            }

            await visits.MarkModifiedAsync(visit, command.ChangedByUserId, token);
        }, cancellationToken);

    private static (string Name, string Dosage, string Frequency, string Duration, string? Instructions) Key(Medication medication) =>
        (medication.MedicineName, medication.Dosage, medication.Frequency, medication.Duration, medication.SpecialInstructions);
}

/// <summary>Suggests medicine names from the doctor's own history. Read-only: no transaction.</summary>
public sealed class MedicineSuggestionsQueryHandler(IMedicineSuggestionRepository suggestions)
{
    public Task<IReadOnlyList<string>> HandleAsync(MedicineSuggestionsQuery query, CancellationToken cancellationToken) =>
        suggestions.FindNamesStartingWithAsync((query.Search ?? string.Empty).Trim(), query.Limit, cancellationToken);
}
