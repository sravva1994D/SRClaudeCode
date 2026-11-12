using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Consultation;
using PatientManagement.Application.Consultation.Handlers;
using PatientManagement.Application.Visits;
using PatientManagement.Application.Visits.Handlers;

namespace PatientManagement.API.Controllers;

/// <summary>Reads visits (consultations) and records their vitals (plan Sprint 7–8, items 32 and 33).</summary>
[ApiController]
[Authorize]
[Route("api/v1/visits")]
public sealed class VisitsController(
    GetVisitQueryHandler getVisit,
    RecordVitalsCommandHandler recordVitals,
    GetVitalsQueryHandler getVitals,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Returns one active visit.</summary>
    [HttpGet("{visitId:int}")]
    [ProducesResponseType<VisitDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VisitDetails>> Get(int visitId, CancellationToken cancellationToken) =>
        Ok(await getVisit.HandleAsync(new GetVisitQuery(visitId), cancellationToken));

    /// <summary>
    /// Records the visit's vitals. Values outside their normal range are saved and returned in <c>warnings</c> (201). A value
    /// outside its plausible range is impossible and returns 400 with code <c>vitals_implausible</c>. A visit that already
    /// has vitals returns 409; a visit that is not in progress returns 422.
    /// </summary>
    [HttpPost("{visitId:int}/vitals")]
    [ProducesResponseType<RecordVitalsResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RecordVitalsResponse>> RecordVitals(
        int visitId,
        [FromBody] RecordVitalsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RecordVitalsCommand(
            visitId,
            request.Temperature,
            request.BloodPressureSystolic,
            request.BloodPressureDiastolic,
            request.Pulse,
            currentUser.UserId);
        var result = await recordVitals.HandleAsync(command, cancellationToken);
        var response = new RecordVitalsResponse(result.Vitals, result.Warnings);
        return CreatedAtAction(nameof(GetVitals), new { visitId }, response);
    }

    /// <summary>Returns the vitals recorded for the visit.</summary>
    [HttpGet("{visitId:int}/vitals")]
    [ProducesResponseType<VitalsDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VitalsDetails>> GetVitals(int visitId, CancellationToken cancellationToken) =>
        Ok(await getVitals.HandleAsync(new GetVitalsQuery(visitId), cancellationToken));
}

/// <summary>Vitals body. Every value is optional; only the values measured are sent.</summary>
public sealed record RecordVitalsRequest(
    decimal? Temperature,
    int? BloodPressureSystolic,
    int? BloodPressureDiastolic,
    int? Pulse);

/// <summary>The saved vitals and the names of any signs outside their normal range.</summary>
public sealed record RecordVitalsResponse(VitalsDetails Vitals, IReadOnlyList<string> Warnings);
