using FluentValidation;
using PatientManagement.Application.Consultation;

namespace PatientManagement.Application.Validators;

/// <summary>
/// Format and length checks for a medications draft. The list must be present and hold at most
/// <see cref="SaveComplaintsCommandValidator.MaximumItemsPerDraft"/> medicines. Each entry is checked for length; the
/// required fields are checked by the domain, which refuses a medicine with a missing dosage, frequency or duration.
/// </summary>
public sealed class SaveMedicationsCommandValidator : AbstractValidator<SaveMedicationsCommand>
{
    public SaveMedicationsCommandValidator()
    {
        RuleFor(command => command.VisitId)
            .GreaterThan(0);

        RuleFor(command => command.Medications)
            .NotNull()
            .WithMessage("The medications list is required.")
            .Must(list => list is null || list.Count <= SaveComplaintsCommandValidator.MaximumItemsPerDraft)
            .WithMessage($"Use at most {SaveComplaintsCommandValidator.MaximumItemsPerDraft} medicines.");

        RuleForEach(command => command.Medications)
            .NotNull()
            .SetValidator(new MedicationEntryValidator());
    }

    private sealed class MedicationEntryValidator : AbstractValidator<MedicationEntry>
    {
        public const int MaximumNameLength = 200;
        public const int MaximumDosageLength = 100;
        public const int MaximumFrequencyLength = 100;
        public const int MaximumDurationLength = 100;
        public const int MaximumInstructionsLength = 500;

        public MedicationEntryValidator()
        {
            RuleFor(entry => entry.MedicineName).MaximumLength(MaximumNameLength);
            RuleFor(entry => entry.Dosage).MaximumLength(MaximumDosageLength);
            RuleFor(entry => entry.Frequency).MaximumLength(MaximumFrequencyLength);
            RuleFor(entry => entry.Duration).MaximumLength(MaximumDurationLength);
            RuleFor(entry => entry.SpecialInstructions).MaximumLength(MaximumInstructionsLength);
        }
    }
}

/// <summary>Format checks for a medicine suggestion request: a short search text and a small result limit.</summary>
public sealed class MedicineSuggestionsQueryValidator : AbstractValidator<MedicineSuggestionsQuery>
{
    public const int MaximumSearchLength = 200;
    public const int MaximumLimit = 20;

    public MedicineSuggestionsQueryValidator()
    {
        RuleFor(query => query.Search)
            .NotNull()
            .MaximumLength(MaximumSearchLength);

        RuleFor(query => query.Limit)
            .InclusiveBetween(1, MaximumLimit);
    }
}
