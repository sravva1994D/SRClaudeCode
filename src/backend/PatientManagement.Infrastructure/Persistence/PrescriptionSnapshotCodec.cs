using System.Text.Json;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Prescriptions;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Stores a printed prescription as JSON (camelCase, the web default). The snapshot holds patient data, so it is never logged.</summary>
public sealed class PrescriptionSnapshotCodec : IPrescriptionSnapshotCodec
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public string Serialize(PrescriptionDocument document) => JsonSerializer.Serialize(document, Options);

    public PrescriptionDocument Deserialize(string snapshot) =>
        JsonSerializer.Deserialize<PrescriptionDocument>(snapshot, Options)
        ?? throw new InvalidOperationException("The stored prescription snapshot could not be read.");
}
