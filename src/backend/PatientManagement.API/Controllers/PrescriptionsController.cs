using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Prescriptions;
using PatientManagement.Application.Prescriptions.Handlers;

namespace PatientManagement.API.Controllers;

/// <summary>Issues the printable prescription for a completed visit and returns its content (plan Sprint 9, item 40).</summary>
[ApiController]
[Authorize]
[Route("api/v1/visits/{visitId:int}/prescription")]
public sealed class PrescriptionsController(
    GeneratePrescriptionCommandHandler generate,
    GetPrescriptionQueryHandler getPrescription,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// Issues the prescription for the visit. The visit must be completed and have at least one medication (422 otherwise).
    /// Returns 201 when issued now, or 200 with the existing prescription when the visit already has one.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<PrescriptionDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<PrescriptionDetails>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PrescriptionDetails>> Generate(int visitId, CancellationToken cancellationToken)
    {
        var result = await generate.HandleAsync(
            new GeneratePrescriptionCommand(visitId, currentUser.UserId),
            cancellationToken);

        if (!result.Created)
        {
            return Ok(result.Prescription);
        }

        return CreatedAtAction(nameof(Get), new { visitId }, result.Prescription);
    }

    /// <summary>Returns the printable content of the visit's prescription, built from the visit's current records.</summary>
    [HttpGet]
    [ProducesResponseType<PrescriptionDocument>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrescriptionDocument>> Get(int visitId, CancellationToken cancellationToken) =>
        Ok(await getPrescription.HandleAsync(new GetPrescriptionQuery(visitId), cancellationToken));
}
