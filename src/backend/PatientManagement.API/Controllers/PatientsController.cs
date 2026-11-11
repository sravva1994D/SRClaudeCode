using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Patients;
using PatientManagement.Application.Patients.Handlers;

namespace PatientManagement.API.Controllers;

/// <summary>Registers, reads, edits and deactivates patients.</summary>
[ApiController]
[Authorize]
[Route("api/v1/patients")]
public sealed class PatientsController(
    GetPatientQueryHandler getPatient,
    CreatePatientCommandHandler createPatient,
    UpdatePatientCommandHandler updatePatient,
    DeletePatientCommandHandler deletePatient,
    IValidator<CreatePatientCommand> createValidator,
    IValidator<UpdatePatientCommand> updateValidator,
    FindDuplicatePatientsQueryHandler findDuplicates,
    IValidator<FindDuplicatePatientsQuery> duplicateQueryValidator,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Returns active patients with the same name and date of birth. Used by the patient form to warn before a
    /// duplicate is saved; the warning does not block the save.
    /// </summary>
    [HttpGet("duplicates")]
    [ProducesResponseType<IReadOnlyList<PatientSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PatientSummary>>> FindDuplicates(
        [FromQuery] string? fullName,
        [FromQuery] DateOnly? dateOfBirth,
        [FromQuery] int? excludePatientId,
        CancellationToken cancellationToken)
    {
        var query = new FindDuplicatePatientsQuery(fullName ?? string.Empty, dateOfBirth ?? default, excludePatientId);
        await duplicateQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        return Ok(await findDuplicates.HandleAsync(query, cancellationToken));
    }

    /// <summary>Returns one active patient.</summary>
    [HttpGet("{patientId:int}")]
    [ProducesResponseType<PatientDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDetails>> Get(int patientId, CancellationToken cancellationToken) =>
        Ok(await getPatient.HandleAsync(patientId, cancellationToken));

    /// <summary>Registers a patient. The insert is audited in the same transaction.</summary>
    [HttpPost]
    [ProducesResponseType<PatientDetails>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PatientDetails>> Create(
        [FromBody] PatientRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePatientCommand(request.ToFields(), currentUser.UserId);
        await createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var created = await createPatient.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(Get), new { patientId = created.PatientId }, created);
    }

    /// <summary>Replaces a patient's details. The change is audited in the same transaction.</summary>
    [HttpPut("{patientId:int}")]
    [ProducesResponseType<PatientDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PatientDetails>> Update(
        int patientId,
        [FromBody] PatientRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePatientCommand(patientId, request.ToFields(), currentUser.UserId);
        await updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        return Ok(await updatePatient.HandleAsync(command, cancellationToken));
    }

    /// <summary>Deactivates a patient. The record and its history are kept.</summary>
    [HttpDelete("{patientId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(int patientId, CancellationToken cancellationToken)
    {
        await deletePatient.HandleAsync(new DeletePatientCommand(patientId, currentUser.UserId), cancellationToken);
        return NoContent();
    }
}

/// <summary>Patient create and update body. Missing required values fail validation with 400.</summary>
public sealed record PatientRequest(
    string? FullName,
    DateOnly? DateOfBirth,
    int? GenderId,
    string? PhoneNumber,
    string? Address)
{
    public PatientFields ToFields() => new(
        FullName ?? string.Empty,
        DateOfBirth ?? default,
        GenderId ?? 0,
        PhoneNumber,
        Address);
}
