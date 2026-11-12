using FluentValidation;
using PatientManagement.Application.Consultation;

namespace PatientManagement.Application.Validators;

/// <summary>
/// Format and length checks for a complaints draft (CLAUDE.md 6.6). The list must be present, each entry at most 2000
/// characters, and the list at most <see cref="MaximumItemsPerDraft"/> entries. The BRD says a visit typically has 1–5
/// complaints; the cap only stops a runaway draft.
/// </summary>
public sealed class SaveComplaintsCommandValidator : AbstractValidator<SaveComplaintsCommand>
{
    public const int MaximumTextLength = 2000;
    public const int MaximumItemsPerDraft = 20;

    public SaveComplaintsCommandValidator()
    {
        RuleFor(command => command.VisitId)
            .GreaterThan(0);

        RuleFor(command => command.Complaints)
            .NotNull()
            .WithMessage("The complaints list is required.")
            .Must(list => list is null || list.Count <= MaximumItemsPerDraft)
            .WithMessage($"Use at most {MaximumItemsPerDraft} complaints.");

        RuleForEach(command => command.Complaints)
            .NotNull()
            .MaximumLength(MaximumTextLength);
    }
}

/// <summary>
/// Format and length checks for a diagnoses draft. The list must be present and at most
/// <see cref="SaveComplaintsCommandValidator.MaximumItemsPerDraft"/> entries; each entry's text is at most 2000 characters
/// and its code at most <see cref="MaximumCodeLength"/> characters.
/// </summary>
public sealed class SaveDiagnosesCommandValidator : AbstractValidator<SaveDiagnosesCommand>
{
    public const int MaximumCodeLength = 20;

    public SaveDiagnosesCommandValidator()
    {
        RuleFor(command => command.VisitId)
            .GreaterThan(0);

        RuleFor(command => command.Diagnoses)
            .NotNull()
            .WithMessage("The diagnoses list is required.")
            .Must(list => list is null || list.Count <= SaveComplaintsCommandValidator.MaximumItemsPerDraft)
            .WithMessage($"Use at most {SaveComplaintsCommandValidator.MaximumItemsPerDraft} diagnoses.");

        RuleForEach(command => command.Diagnoses)
            .NotNull()
            .SetValidator(new DiagnosisEntryValidator());
    }

    private sealed class DiagnosisEntryValidator : AbstractValidator<DiagnosisEntry>
    {
        public DiagnosisEntryValidator()
        {
            RuleFor(entry => entry.Text)
                .MaximumLength(SaveComplaintsCommandValidator.MaximumTextLength);

            RuleFor(entry => entry.Code)
                .MaximumLength(MaximumCodeLength);
        }
    }
}
