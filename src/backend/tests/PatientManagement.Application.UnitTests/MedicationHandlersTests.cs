using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Consultation;
using PatientManagement.Application.Consultation.Handlers;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Consultation;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Visits;

namespace PatientManagement.Application.UnitTests;

public sealed class MedicationHandlersTests
{
    private readonly IVisitRepository _visits = Substitute.For<IVisitRepository>();
    private readonly IMedicationRepository _medications = Substitute.For<IMedicationRepository>();
    private readonly IMedicineSuggestionRepository _suggestions = Substitute.For<IMedicineSuggestionRepository>();

    [Fact]
    public async Task SaveMedications_ChangedDraft_ReplacesTheSavedMedications()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        _medications.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(new List<Medication> { Medication.Reconstitute(1, 3, "Paracetamol", "500mg", "Twice daily", "7 days", null, 0, true, [1]) });
        var handler = new SaveMedicationsCommandHandler(new InlineUnitOfWork(), _visits, _medications);

        await handler.HandleAsync(
            new SaveMedicationsCommand(3, [new MedicationEntry("Amoxicillin", "250mg", "Thrice daily", "5 days", "After food")], 1),
            CancellationToken.None);

        await _medications.Received(1).DeactivateAsync(
            Arg.Is<Medication>(medication => medication.MedicineName == "Paracetamol"), 1, Arg.Any<CancellationToken>());
        await _medications.Received(1).AddAsync(
            Arg.Is<Medication>(medication => medication.MedicineName == "Amoxicillin" && medication.SortOrder == 0),
            1,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveMedications_UnchangedDraft_WritesNothing()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        _medications.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(new List<Medication> { Medication.Reconstitute(1, 3, "Paracetamol", "500mg", "Twice daily", "7 days", null, 0, true, [1]) });
        var handler = new SaveMedicationsCommandHandler(new InlineUnitOfWork(), _visits, _medications);

        await handler.HandleAsync(
            new SaveMedicationsCommand(3, [new MedicationEntry(" Paracetamol ", "500mg", "Twice daily", "7 days", null)], 1),
            CancellationToken.None);

        await _medications.DidNotReceive().DeactivateAsync(Arg.Any<Medication>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _medications.DidNotReceive().AddAsync(Arg.Any<Medication>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveMedications_MedicineMissingDosage_ThrowsAndWritesNothing()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        _medications.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>()).Returns(new List<Medication>());
        var handler = new SaveMedicationsCommandHandler(new InlineUnitOfWork(), _visits, _medications);

        var act = () => handler.HandleAsync(
            new SaveMedicationsCommand(3, [new MedicationEntry("Paracetamol", "", "Twice daily", "7 days", null)], 1),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().Where(error => error.ErrorCode == "medication_dosage_required");
        await _medications.DidNotReceive().AddAsync(Arg.Any<Medication>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveMedications_CompletedVisit_ThrowsAndWritesNothing()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]));
        var handler = new SaveMedicationsCommandHandler(new InlineUnitOfWork(), _visits, _medications);

        var act = () => handler.HandleAsync(
            new SaveMedicationsCommand(3, [new MedicationEntry("Paracetamol", "500mg", "Twice daily", "7 days", null)], 1),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().Where(error => error.ErrorCode == "visit_not_in_progress");
        await _medications.DidNotReceive().AddAsync(Arg.Any<Medication>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveMedications_EntryWithBlankName_IsDropped()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        _medications.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>()).Returns(new List<Medication>());
        var handler = new SaveMedicationsCommandHandler(new InlineUnitOfWork(), _visits, _medications);

        await handler.HandleAsync(
            new SaveMedicationsCommand(3, [new MedicationEntry("  ", null, null, null, null)], 1),
            CancellationToken.None);

        await _medications.DidNotReceive().AddAsync(Arg.Any<Medication>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MedicineSuggestions_SearchWithSpaces_TrimsTheSearchAndPassesTheLimit()
    {
        _suggestions.FindNamesStartingWithAsync("Par", 5, Arg.Any<CancellationToken>())
            .Returns(new List<string> { "Paracetamol" });
        var handler = new MedicineSuggestionsQueryHandler(_suggestions);

        var names = await handler.HandleAsync(new MedicineSuggestionsQuery("  Par ", 5), CancellationToken.None);

        names.Should().Equal("Paracetamol");
    }
}
