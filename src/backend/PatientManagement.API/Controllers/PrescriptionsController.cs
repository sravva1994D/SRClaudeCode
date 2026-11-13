using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Prescriptions;
using PatientManagement.Application.Prescriptions.Handlers;

namespace PatientManagement.API.Controllers;

/// <summary>
/// Issues the printable prescription for a completed visit (plan Sprint 9, item 40), returns its content, and records each
/// print with a snapshot so a reprint shows what was printed (item 41).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/visits/{visitId:int}/prescription")]
public sealed class PrescriptionsController(
    GeneratePrescriptionCommandHandler generate,
    GetPrescriptionQueryHandler getPrescription,
    RecordPrescriptionPrintCommandHandler recordPrint,
    GetPrescriptionPrintsQueryHandler getPrints,
    GetPrescriptionPrintQueryHandler getPrint,
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

    /// <summary>
    /// Records a print of the prescription with the content as it is printed now. The snapshot is stored with the print, so
    /// later edits to the visit do not change this print. Returns 201 with the print and its content.
    /// </summary>
    [HttpPost("prints")]
    [ProducesResponseType<PrescriptionPrintDetails>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrescriptionPrintDetails>> RecordPrint(int visitId, CancellationToken cancellationToken)
    {
        var printed = await recordPrint.HandleAsync(
            new RecordPrescriptionPrintCommand(visitId, currentUser.UserId),
            cancellationToken);

        return CreatedAtAction(nameof(GetPrint), new { visitId, printId = printed.PrescriptionPrintId }, printed);
    }

    /// <summary>Lists the prints of the visit's prescription, oldest first, for the reprint list.</summary>
    [HttpGet("prints")]
    [ProducesResponseType<IReadOnlyList<PrescriptionPrintSummary>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PrescriptionPrintSummary>>> ListPrints(
        int visitId,
        CancellationToken cancellationToken) =>
        Ok(await getPrints.HandleAsync(new GetPrescriptionPrintsQuery(visitId), cancellationToken));

    /// <summary>Returns one print with the content that was printed (the reprint), read from its snapshot.</summary>
    [HttpGet("prints/{printId:int}")]
    [ProducesResponseType<PrescriptionPrintDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrescriptionPrintDetails>> GetPrint(int visitId, int printId, CancellationToken cancellationToken) =>
        Ok(await getPrint.HandleAsync(new GetPrescriptionPrintQuery(visitId, printId), cancellationToken));
}
