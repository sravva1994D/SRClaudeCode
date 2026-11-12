using FluentAssertions;
using PatientManagement.Domain.Consultation;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Domain.UnitTests;

public sealed class MedicationTests
{
    [Fact]
    public void Record_FieldsWithSurroundingSpaces_AreTrimmed()
    {
        var medication = Medication.Record(3, "  Paracetamol ", " 500mg ", " Twice daily ", " 7 days ", " After food ", 0);

        medication.MedicineName.Should().Be("Paracetamol");
        medication.Dosage.Should().Be("500mg");
        medication.Frequency.Should().Be("Twice daily");
        medication.Duration.Should().Be("7 days");
        medication.SpecialInstructions.Should().Be("After food");
    }

    [Fact]
    public void Record_BlankInstructions_StoresNone()
    {
        var medication = Medication.Record(3, "Paracetamol", "500mg", "Twice daily", "7 days", "   ", 0);

        medication.SpecialInstructions.Should().BeNull();
    }

    [Theory]
    [InlineData("", "500mg", "Twice daily", "7 days", "medication_name_required")]
    [InlineData("Paracetamol", "  ", "Twice daily", "7 days", "medication_dosage_required")]
    [InlineData("Paracetamol", "500mg", "", "7 days", "medication_frequency_required")]
    [InlineData("Paracetamol", "500mg", "Twice daily", " ", "medication_duration_required")]
    public void Record_MissingRequiredField_ThrowsWithItsCode(
        string name,
        string dosage,
        string frequency,
        string duration,
        string expectedCode)
    {
        var act = () => Medication.Record(3, name, dosage, frequency, duration, null, 0);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be(expectedCode);
    }

    [Fact]
    public void Deactivate_Medication_MarksItInactive()
    {
        var medication = Medication.Reconstitute(1, 3, "Paracetamol", "500mg", "Twice daily", "7 days", null, 0, true, [1]);

        medication.Deactivate();

        medication.IsActive.Should().BeFalse();
    }
}
