namespace PatientManagement.Domain.Exceptions;

/// <summary>The requested 15-minute slot already has an active appointment. The API maps it to 409 (CLAUDE.md 6.5).</summary>
public sealed class SlotAlreadyBookedException()
    : DomainException("slot_already_booked", "That time slot is already booked.")
{
}
