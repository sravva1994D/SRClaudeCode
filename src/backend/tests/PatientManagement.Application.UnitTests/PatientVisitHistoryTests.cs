using FluentAssertions;
using FluentValidation.TestHelper;
using NSubstitute;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Paging;
using PatientManagement.Application.Validators;
using PatientManagement.Application.Visits;
using PatientManagement.Application.Visits.Handlers;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;

namespace PatientManagement.Application.UnitTests;

public sealed class PatientVisitHistoryTests
{
    private readonly IPatientRepository _patients = Substitute.For<IPatientRepository>();
    private readonly IVisitHistoryRepository _history = Substitute.For<IVisitHistoryRepository>();

    [Fact]
    public async Task HandleAsync_ActivePatient_ReturnsThePageFromTheRepository()
    {
        _patients.FindActiveAsync(7, Arg.Any<CancellationToken>())
            .Returns(Patient.Reconstitute(7, "Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, true, [1], null, null));
        var page = new PagedResponse<PatientVisitHistoryItem>(
            [new PatientVisitHistoryItem(3, new DateTime(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc), 2, new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc), 9)],
            TotalCount: 1,
            PageNumber: 1,
            PageSize: 20);
        _history.ListForPatientAsync(Arg.Any<PatientVisitHistoryQuery>(), Arg.Any<CancellationToken>()).Returns(page);
        var handler = new PatientVisitHistoryHandler(_patients, _history);
        var query = new PatientVisitHistoryQuery(7, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), 1, 20);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(item => item.VisitId == 3);
        await _history.Received(1).ListForPatientAsync(query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownPatient_ThrowsNotFoundWithoutQueryingVisits()
    {
        _patients.FindActiveAsync(99, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        var handler = new PatientVisitHistoryHandler(_patients, _history);

        var act = () => handler.HandleAsync(
            new PatientVisitHistoryQuery(99, null, null, 1, 20),
            CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
        await _history.DidNotReceive().ListForPatientAsync(Arg.Any<PatientVisitHistoryQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_EndDateBeforeStartDate_IsRefused()
    {
        var validator = new PatientVisitHistoryValidator();

        var result = validator.TestValidate(
            new PatientVisitHistoryQuery(7, new DateOnly(2026, 11, 30), new DateOnly(2026, 11, 1), 1, 20));

        result.ShouldHaveValidationErrorFor(query => query.To);
    }

    [Fact]
    public void Validator_OpenEndedRange_IsAccepted()
    {
        var validator = new PatientVisitHistoryValidator();

        var result = validator.TestValidate(new PatientVisitHistoryQuery(7, new DateOnly(2026, 11, 1), null, 1, 20));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_PageSizeAboveTheCap_IsRefused()
    {
        var validator = new PatientVisitHistoryValidator();

        var result = validator.TestValidate(new PatientVisitHistoryQuery(7, null, null, 1, PagingLimits.MaximumPageSize + 1));

        result.ShouldHaveValidationErrorFor(query => query.PageSize);
    }
}
