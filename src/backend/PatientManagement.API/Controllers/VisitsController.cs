using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Consultation;
using PatientManagement.Application.Consultation.Handlers;
using PatientManagement.Application.Visits;
using PatientManagement.Application.Visits.Handlers;

namespace PatientManagement.API.Controllers;

/// <summary>
/// Reads visits (consultations), records their vitals (plan Sprint 7–8, item 33), and saves their complaints and
/// diagnoses as drafts (item 34).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/visits")]
public sealed class VisitsController(
    GetVisitQueryHandler getVisit,
    RecordVitalsCommandHandler recordVitals,
    GetVitalsQueryHandler getVitals,
    SaveComplaintsCommandHandler saveComplaints,
    IValidator<SaveComplaintsCommand> saveComplaintsValidator,
    SaveDiagnosesCommandHandler saveDiagnoses,
    IValidator<SaveDiagnosesCommand> saveDiagnosesValidator,
    GetVisitNotesQueryHandler getVisitNotes,
    SaveMedicationsCommandHandler saveMedications,
    IValidator<SaveMedicationsCommand> saveMedicationsValidator,
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

    /// <summary>Returns the complaints and diagnoses saved for the visit.</summary>
    [HttpGet("{visitId:int}/notes")]
    [ProducesResponseType<VisitNotesDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VisitNotesDetails>> GetNotes(int visitId, CancellationToken cancellationToken) =>
        Ok(await getVisitNotes.HandleAsync(new GetVisitNotesQuery(visitId), cancellationToken));

    /// <summary>
    /// Saves the visit's complaints as a draft: the list replaces the saved complaints. Blank entries are dropped. An
    /// unchanged list writes nothing. A visit that is not in progress returns 422.
    /// </summary>
    [HttpPut("{visitId:int}/complaints")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SaveComplaints(
        int visitId,
        [FromBody] SaveComplaintsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SaveComplaintsCommand(visitId, request.Complaints, currentUser.UserId);
        await saveComplaintsValidator.ValidateAndThrowAsync(command, cancellationToken);
        await saveComplaints.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Saves the visit's diagnoses as a draft: the list replaces the saved diagnoses. Entries with blank text are dropped.
    /// An unchanged list writes nothing. A visit that is not in progress returns 422.
    /// </summary>
    [HttpPut("{visitId:int}/diagnoses")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SaveDiagnoses(
        int visitId,
        [FromBody] SaveDiagnosesRequest request,
        CancellationToken cancellationToken)
    {
        var entries = request.Diagnoses?.Select(entry => new DiagnosisEntry(entry.Text, entry.Code)).ToList();
        var command = new SaveDiagnosesCommand(visitId, entries, currentUser.UserId);
        await saveDiagnosesValidator.ValidateAndThrowAsync(command, cancellationToken);
        await saveDiagnoses.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Saves the visit's medications as a draft: the list replaces the saved medications. An entry with a blank name is
    /// dropped; an entry missing its dosage, frequency or duration returns 422. An unchanged list writes nothing. A visit
    /// that is not in progress returns 422.
    /// </summary>
    [HttpPut("{visitId:int}/medications")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SaveMedications(
        int visitId,
        [FromBody] SaveMedicationsRequest request,
        CancellationToken cancellationToken)
    {
        var entries = request.Medications?.Select(entry => new MedicationEntry(
            entry.MedicineName,
            entry.Dosage,
            entry.Frequency,
            entry.Duration,
            entry.SpecialInstructions)).ToList();
        var command = new SaveMedicationsCommand(visitId, entries, currentUser.UserId);
        await saveMedicationsValidator.ValidateAndThrowAsync(command, cancellationToken);
        await saveMedications.HandleAsync(command, cancellationToken);
        return NoContent();
    }
}

/// <summary>Complaints draft body. The list is required; an empty list clears the complaints.</summary>
public sealed record SaveComplaintsRequest(IReadOnlyList<string>? Complaints);

/// <summary>One diagnosis entry in a draft: the notes and an optional reference code.</summary>
public sealed record DiagnosisEntryRequest(string? Text, string? Code);

/// <summary>Diagnoses draft body. The list is required; an empty list clears the diagnoses.</summary>
public sealed record SaveDiagnosesRequest(IReadOnlyList<DiagnosisEntryRequest>? Diagnoses);

/// <summary>One medicine in a draft: name, dosage, frequency, duration, and an optional instruction.</summary>
public sealed record MedicationEntryRequest(
    string? MedicineName,
    string? Dosage,
    string? Frequency,
    string? Duration,
    string? SpecialInstructions);

/// <summary>Medications draft body. The list is required; an empty list clears the medications.</summary>
public sealed record SaveMedicationsRequest(IReadOnlyList<MedicationEntryRequest>? Medications);

/// <summary>Vitals body. Every value is optional; only the values measured are sent.</summary>
public sealed record RecordVitalsRequest(
    decimal? Temperature,
    int? BloodPressureSystolic,
    int? BloodPressureDiastolic,
    int? Pulse);

/// <summary>The saved vitals and the names of any signs outside their normal range.</summary>
public sealed record RecordVitalsResponse(VitalsDetails Vitals, IReadOnlyList<string> Warnings);
