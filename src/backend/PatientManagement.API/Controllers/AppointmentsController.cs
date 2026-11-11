using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Appointments;
using PatientManagement.Application.Appointments.Handlers;
using PatientManagement.Application.Interfaces;

namespace PatientManagement.API.Controllers;

/// <summary>Books and reads appointments in fixed 15-minute slots (plan Sprint 5–6, item 25).</summary>
[ApiController]
[Authorize]
[Route("api/v1/appointments")]
public sealed class AppointmentsController(
    ScheduleAppointmentCommandHandler schedule,
    GetAppointmentQueryHandler getAppointment,
    ListAppointmentsQueryHandler listAppointments,
    IValidator<ScheduleAppointmentCommand> scheduleValidator,
    IValidator<ListAppointmentsQuery> listValidator,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Returns the appointments whose slot starts in [from, to), for the calendar. Both times are UTC; a time without an
    /// offset is read as UTC. The range can be at most 31 days.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AppointmentListItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<AppointmentListItem>>> List(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var query = new ListAppointmentsQuery(AsUtc(from), AsUtc(to));
        await listValidator.ValidateAndThrowAsync(query, cancellationToken);

        return Ok(await listAppointments.HandleAsync(query, cancellationToken));
    }

    /// <summary>Returns one active appointment.</summary>
    [HttpGet("{appointmentId:int}")]
    [ProducesResponseType<AppointmentDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDetails>> Get(int appointmentId, CancellationToken cancellationToken) =>
        Ok(await getAppointment.HandleAsync(new GetAppointmentQuery(appointmentId), cancellationToken));

    /// <summary>
    /// Books a 15-minute slot for a patient. The slot start is UTC; a time without an offset is read as UTC. A slot that
    /// is not on the quarter hour, or is not in the future, returns 422. A slot already booked returns 409.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<AppointmentDetails>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AppointmentDetails>> Schedule(
        [FromBody] ScheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(currentUser.UserId);
        await scheduleValidator.ValidateAndThrowAsync(command, cancellationToken);

        var booked = await schedule.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { appointmentId = booked.AppointmentId }, booked);
    }

    /// <summary>A time without an offset is read as UTC, so the server never interprets it in local time.</summary>
    private static DateTime AsUtc(DateTime? value)
    {
        var time = value ?? default;
        return time.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(time, DateTimeKind.Utc) : time.ToUniversalTime();
    }
}

/// <summary>Booking body. Missing values fail validation with 400.</summary>
public sealed record ScheduleAppointmentRequest(int? PatientId, DateTime? SlotStart)
{
    public ScheduleAppointmentCommand ToCommand(int changedByUserId)
    {
        var slot = SlotStart ?? default;
        var slotUtc = slot.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(slot, DateTimeKind.Utc)
            : slot.ToUniversalTime();
        return new ScheduleAppointmentCommand(PatientId ?? 0, slotUtc, changedByUserId);
    }
}
