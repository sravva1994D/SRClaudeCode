using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Clinic;
using PatientManagement.Application.Clinic.Handlers;
using PatientManagement.Application.Interfaces;

namespace PatientManagement.API.Controllers;

/// <summary>Reads and updates the clinic profile shown in the prescription header (Q4).</summary>
[ApiController]
[Authorize]
[Route("api/v1/clinic-profile")]
public sealed class ClinicProfileController(
    GetClinicProfileQueryHandler getProfile,
    UpdateClinicProfileCommandHandler updateProfile,
    IValidator<UpdateClinicProfileCommand> validator,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Returns the clinic profile.</summary>
    [HttpGet]
    [ProducesResponseType<ClinicProfileDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClinicProfileDetails>> Get(CancellationToken cancellationToken) =>
        Ok(await getProfile.HandleAsync(cancellationToken));

    /// <summary>Replaces the clinic profile. The change is audited in the same transaction.</summary>
    [HttpPut]
    [ProducesResponseType<ClinicProfileDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClinicProfileDetails>> Update(
        [FromBody] UpdateClinicProfileRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateClinicProfileCommand(
            request.ClinicName,
            request.DoctorName,
            request.Qualifications,
            request.RegistrationNumber,
            request.Address,
            request.PhoneNumber,
            request.FooterNotes,
            currentUser.UserId);
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        return Ok(await updateProfile.HandleAsync(command, cancellationToken));
    }
}

/// <summary>Clinic profile update body. Empty text clears a field.</summary>
public sealed record UpdateClinicProfileRequest(
    string? ClinicName,
    string? DoctorName,
    string? Qualifications,
    string? RegistrationNumber,
    string? Address,
    string? PhoneNumber,
    string? FooterNotes);
