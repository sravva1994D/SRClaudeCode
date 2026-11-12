using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Consultation;
using PatientManagement.Application.Consultation.Handlers;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Consultation;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Visits;

namespace PatientManagement.Application.UnitTests;

public sealed class VisitNotesHandlersTests
{
    private readonly IVisitRepository _visits = Substitute.For<IVisitRepository>();
    private readonly IComplaintRepository _complaints = Substitute.For<IComplaintRepository>();
    private readonly IDiagnosisRepository _diagnoses = Substitute.For<IDiagnosisRepository>();
    private readonly IMedicationRepository _medications = Substitute.For<IMedicationRepository>();

    [Fact]
    public async Task SaveComplaints_ChangedDraft_ReplacesTheSavedComplaints()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        _complaints.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(new List<Complaint> { Complaint.Reconstitute(1, 3, "Cough", 0, true, [1]) });
        var handler = new SaveComplaintsCommandHandler(new InlineUnitOfWork(), _visits, _complaints);

        await handler.HandleAsync(new SaveComplaintsCommand(3, ["Fever", "  "], 1), CancellationToken.None);

        await _complaints.Received(1).DeactivateAsync(
            Arg.Is<Complaint>(complaint => complaint.Text == "Cough"), 1, Arg.Any<CancellationToken>());
        await _complaints.Received(1).AddAsync(
            Arg.Is<Complaint>(complaint => complaint.Text == "Fever" && complaint.SortOrder == 0),
            1,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveComplaints_UnchangedDraft_WritesNothing()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        _complaints.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(new List<Complaint> { Complaint.Reconstitute(1, 3, "Cough", 0, true, [1]) });
        var handler = new SaveComplaintsCommandHandler(new InlineUnitOfWork(), _visits, _complaints);

        await handler.HandleAsync(new SaveComplaintsCommand(3, [" Cough "], 1), CancellationToken.None);

        await _complaints.DidNotReceive().DeactivateAsync(Arg.Any<Complaint>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _complaints.DidNotReceive().AddAsync(Arg.Any<Complaint>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _visits.DidNotReceive().MarkModifiedAsync(Arg.Any<Visit>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveComplaints_CompletedVisit_SavesAndStampsTheVisit()
    {
        var visit = Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]);
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>()).Returns(visit);
        _complaints.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>()).Returns(new List<Complaint>());
        var handler = new SaveComplaintsCommandHandler(new InlineUnitOfWork(), _visits, _complaints);

        await handler.HandleAsync(new SaveComplaintsCommand(3, ["Fever"], 1), CancellationToken.None);

        await _complaints.Received(1).AddAsync(
            Arg.Is<Complaint>(complaint => complaint.Text == "Fever"), 1, Arg.Any<CancellationToken>());
        await _visits.Received(1).MarkModifiedAsync(visit, 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveComplaints_UnknownVisit_ThrowsNotFound()
    {
        _visits.FindActiveForUpdateAsync(99, Arg.Any<CancellationToken>()).Returns((Visit?)null);
        var handler = new SaveComplaintsCommandHandler(new InlineUnitOfWork(), _visits, _complaints);

        var act = () => handler.HandleAsync(new SaveComplaintsCommand(99, ["Fever"], 1), CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
    }

    [Fact]
    public async Task SaveDiagnoses_CodeOnlyChange_IsSaved()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        _diagnoses.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(new List<Diagnosis> { Diagnosis.Reconstitute(1, 3, "Viral fever", null, 0, true, [1]) });
        var handler = new SaveDiagnosesCommandHandler(new InlineUnitOfWork(), _visits, _diagnoses);

        await handler.HandleAsync(
            new SaveDiagnosesCommand(3, [new DiagnosisEntry("Viral fever", "J11")], 1),
            CancellationToken.None);

        await _diagnoses.Received(1).AddAsync(
            Arg.Is<Diagnosis>(diagnosis => diagnosis.Code == "J11"), 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveDiagnoses_EntryWithBlankText_IsDroppedAndTheDraftIsUnchanged()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        _diagnoses.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(new List<Diagnosis> { Diagnosis.Reconstitute(1, 3, "Viral fever", null, 0, true, [1]) });
        var handler = new SaveDiagnosesCommandHandler(new InlineUnitOfWork(), _visits, _diagnoses);

        await handler.HandleAsync(
            new SaveDiagnosesCommand(3, [new DiagnosisEntry("  ", "X"), new DiagnosisEntry("Viral fever", null)], 1),
            CancellationToken.None);

        await _diagnoses.DidNotReceive().AddAsync(Arg.Any<Diagnosis>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVisitNotes_SavedNotes_ReturnsThemInEntryOrder()
    {
        _visits.FindActiveAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        _complaints.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(new List<Complaint>
            {
                Complaint.Reconstitute(1, 3, "Cough", 0, true, [1]),
                Complaint.Reconstitute(2, 3, "Fever", 1, true, [1]),
            });
        _diagnoses.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(new List<Diagnosis> { Diagnosis.Reconstitute(1, 3, "Viral fever", "J11", 0, true, [1]) });
        var handler = new GetVisitNotesQueryHandler(_visits, _complaints, _diagnoses, _medications);

        var notes = await handler.HandleAsync(new GetVisitNotesQuery(3), CancellationToken.None);

        notes.Complaints.Should().Equal("Cough", "Fever");
        notes.Diagnoses.Should().ContainSingle().Which.Should().Be(new DiagnosisDetails("Viral fever", "J11"));
    }
}
