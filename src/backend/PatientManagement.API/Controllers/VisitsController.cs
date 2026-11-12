using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Visits;
using PatientManagement.Application.Visits.Handlers;

namespace PatientManagement.API.Controllers;

/// <summary>Reads visits (consultations) (plan Sprint 7–8, item 32).</summary>
[ApiController]
[Authorize]
[Route("api/v1/visits")]
public sealed class VisitsController(GetVisitQueryHandler getVisit) : ControllerBase
{
    /// <summary>Returns one active visit.</summary>
    [HttpGet("{visitId:int}")]
    [ProducesResponseType<VisitDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VisitDetails>> Get(int visitId, CancellationToken cancellationToken) =>
        Ok(await getVisit.HandleAsync(new GetVisitQuery(visitId), cancellationToken));
}
