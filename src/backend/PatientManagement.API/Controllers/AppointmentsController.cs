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
    IValidator<ScheduleAppointmentCommand> scheduleValidator,
    ICurrentUser currentUser) : ControllerBase
{
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
