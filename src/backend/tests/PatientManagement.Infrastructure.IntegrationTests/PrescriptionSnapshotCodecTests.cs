using FluentAssertions;
using PatientManagement.Application.Prescriptions;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class PrescriptionSnapshotCodecTests
{
    [Fact]
    public void Deserialize_WhatWasSerialized_ReturnsTheSameContent()
    {
        var codec = new PrescriptionSnapshotCodec();
        var document = new PrescriptionDocument(
            new PrescriptionDetails(9, 3, new DateOnly(2026, 11, 12)),
            new PrescriptionClinicHeader("Riverside Clinic", "Dr. Test", "MBBS", "REG-1", null, "9000000000", "Call if fever persists"),
            new PrescriptionPatient("Asha Rao", new DateOnly(1990, 5, 14), "9876543210"),
            new PrescriptionVitals(36.6m, 120, 80, 72),
            [new PrescriptionDiagnosis("Viral fever", "J11")],
            [new PrescriptionMedication("Paracetamol", "500mg", "Twice daily", "7 days", "After food")]);

        var restored = codec.Deserialize(codec.Serialize(document));

        restored.Should().BeEquivalentTo(document);
    }
}
