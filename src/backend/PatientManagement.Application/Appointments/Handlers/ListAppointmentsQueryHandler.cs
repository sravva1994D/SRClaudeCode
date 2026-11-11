using PatientManagement.Application.Interfaces;

namespace PatientManagement.Application.Appointments.Handlers;

/// <summary>Returns the appointments in a time range, for the calendar.</summary>
public sealed class ListAppointmentsQueryHandler(IAppointmentRepository appointments)
{
    public Task<IReadOnlyList<AppointmentListItem>> HandleAsync(
        ListAppointmentsQuery query,
        CancellationToken cancellationToken) =>
        appointments.ListActiveBetweenAsync(query.FromUtc, query.ToUtc, cancellationToken);
}
