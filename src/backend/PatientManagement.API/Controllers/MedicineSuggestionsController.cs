using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Consultation;
using PatientManagement.Application.Consultation.Handlers;

namespace PatientManagement.API.Controllers;

/// <summary>Suggests medicine names for the medication entry, from the doctor's own history (plan Sprint 7–8, item 35).</summary>
[ApiController]
[Authorize]
[Route("api/v1/medicines")]
public sealed class MedicineSuggestionsController(
    MedicineSuggestionsQueryHandler suggestions,
    IValidator<MedicineSuggestionsQuery> validator) : ControllerBase
{
    /// <summary>
    /// Returns up to <c>limit</c> distinct medicine names that start with <c>search</c>, in alphabetical order. Only names
    /// from active medications count. The limit is 1 to 20 and defaults to 10.
    /// </summary>
    [HttpGet("suggestions")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetSuggestions(
        [FromQuery] string search = "",
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new MedicineSuggestionsQuery(search, limit);
        await validator.ValidateAndThrowAsync(query, cancellationToken);
        return Ok(await suggestions.HandleAsync(query, cancellationToken));
    }
}
