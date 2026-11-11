using Microsoft.AspNetCore.Mvc;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.API.IntegrationTests.Support;

/// <summary>Raises failures on demand so the error middleware can be tested end to end. Added only by the test host.</summary>
[ApiController]
[Route("api/v1/test-failures")]
public sealed class TestFailuresController : ControllerBase
{
    [HttpGet("domain")]
    public IActionResult RaiseDomainFailure() =>
        throw new DomainException("appointment_in_past", "The appointment is in the past.");

    [HttpGet("unexpected")]
    public IActionResult RaiseUnexpectedFailure() =>
        throw new InvalidOperationException("Internal detail that must not reach the client.");
}
