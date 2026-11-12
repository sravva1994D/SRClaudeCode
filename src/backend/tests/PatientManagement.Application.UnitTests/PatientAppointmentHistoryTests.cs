using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Appointments;
using PatientManagement.Application.Appointments.Handlers;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Paging;
using PatientManagement.Application.Validators;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;

namespace PatientManagement.Application.UnitTests;

public sealed class PatientAppointmentHistoryTests
{
    private readonly IPatientRepository _patients = Substitute.For<IPatientRepository>();
    private readonly IAppointmentRepository _appointments = Substitute.For<IAppointmentRepository>();

    [Fact]
    public async Task HandleAsync_UnknownPatient_ThrowsAndReadsNoAppointments()
    {
        _patients.FindActiveAsync(99, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        var handler = new PatientAppointmentHistoryHandler(_patients, _appointments);

        var act = () => handler.HandleAsync(new PatientAppointmentHistoryQuery(99, 1, 20), CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
        await _appointments.DidNotReceive().ListForPatientAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ActivePatient_ReturnsTheRequestedPage()
    {
        _patients.FindActiveAsync(7, Arg.Any<CancellationToken>())
            .Returns(Patient.Reconstitute(7, "Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, true, [1]));
        var page = new PagedResponse<PatientAppointmentHistoryItem>(
            [new PatientAppointmentHistoryItem(5, new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc), 1)],
            1,
            2,
            20);
        _appointments.ListForPatientAsync(7, 2, 20, Arg.Any<CancellationToken>()).Returns(page);
        var handler = new PatientAppointmentHistoryHandler(_patients, _appointments);

        var result = await handler.HandleAsync(new PatientAppointmentHistoryQuery(7, 2, 20), CancellationToken.None);

        result.Should().Be(page);
    }

    [Fact]
    public void Validate_PageSizeAboveTheMaximum_HasPageSizeError()
    {
        var result = new PatientAppointmentHistoryValidator().Validate(new PatientAppointmentHistoryQuery(7, 1, 101));

        result.Errors.Should().Contain(error => error.PropertyName == nameof(PatientAppointmentHistoryQuery.PageSize));
    }

    [Fact]
    public void Validate_PageNumberZero_HasPageNumberError()
    {
        var result = new PatientAppointmentHistoryValidator().Validate(new PatientAppointmentHistoryQuery(7, 0, 20));

        result.Errors.Should().Contain(error => error.PropertyName == nameof(PatientAppointmentHistoryQuery.PageNumber));
    }
}
