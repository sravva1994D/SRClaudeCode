namespace PatientManagement.Application.Consultation;

/// <summary>
/// Saves the visit's complaints as a draft: the list replaces the visit's active complaints. Blank entries are dropped.
/// A null list is refused by the validator, so it never clears the complaints by accident.
/// </summary>
public sealed record SaveComplaintsCommand(int VisitId, IReadOnlyList<string>? Complaints, int ChangedByUserId);

/// <summary>One diagnosis entry as entered: the notes and an optional reference code.</summary>
public sealed record DiagnosisEntry(string? Text, string? Code);

/// <summary>
/// Saves the visit's diagnoses as a draft: the list replaces the visit's active diagnoses. Entries with blank text are
/// dropped. A null list is refused by the validator.
/// </summary>
public sealed record SaveDiagnosesCommand(int VisitId, IReadOnlyList<DiagnosisEntry>? Diagnoses, int ChangedByUserId);

public sealed record GetVisitNotesQuery(int VisitId);

/// <summary>A saved diagnosis as the API returns it.</summary>
public sealed record DiagnosisDetails(string Text, string? Code);

/// <summary>One medicine as entered: the name, dosage, frequency, duration, and an optional instruction.</summary>
public sealed record MedicationEntry(
    string? MedicineName,
    string? Dosage,
    string? Frequency,
    string? Duration,
    string? SpecialInstructions);

/// <summary>
/// Saves the visit's medications as a draft: the list replaces the visit's active medications. Entries with a blank
/// medicine name are dropped; the other required fields are checked by the domain. A null list is refused by the validator.
/// </summary>
public sealed record SaveMedicationsCommand(int VisitId, IReadOnlyList<MedicationEntry>? Medications, int ChangedByUserId);

/// <summary>A saved medication as the API returns it.</summary>
public sealed record MedicationDetails(
    string MedicineName,
    string Dosage,
    string Frequency,
    string Duration,
    string? SpecialInstructions);

/// <summary>
/// The complaints, diagnoses and medications saved for a visit, in the order they were entered.
/// </summary>
public sealed record VisitNotesDetails(
    int VisitId,
    IReadOnlyList<string> Complaints,
    IReadOnlyList<DiagnosisDetails> Diagnoses,
    IReadOnlyList<MedicationDetails> Medications);

/// <summary>Asks for medicine names from the doctor's own history that start with the search text.</summary>
public sealed record MedicineSuggestionsQuery(string Search, int Limit);
