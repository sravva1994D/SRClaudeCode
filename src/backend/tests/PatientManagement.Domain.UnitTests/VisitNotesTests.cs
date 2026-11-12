using FluentAssertions;
using PatientManagement.Domain.Consultation;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Domain.UnitTests;

public sealed class VisitNotesTests
{
    [Fact]
    public void Record_ComplaintWithSurroundingSpaces_IsTrimmed()
    {
        var complaint = Complaint.Record(3, "  Cough for three days  ", 0);

        complaint.Text.Should().Be("Cough for three days");
    }

    [Fact]
    public void Record_BlankComplaint_ThrowsComplaintBlank()
    {
        var act = () => Complaint.Record(3, "   ", 0);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("complaint_blank");
    }

    [Fact]
    public void Record_DiagnosisWithBlankCode_StoresNoCode()
    {
        var diagnosis = Diagnosis.Record(3, "Viral fever", "   ", 0);

        diagnosis.Code.Should().BeNull();
    }

    [Fact]
    public void Record_DiagnosisWithCode_KeepsTheTrimmedCode()
    {
        var diagnosis = Diagnosis.Record(3, "Viral fever", " J11 ", 0);

        diagnosis.Code.Should().Be("J11");
    }

    [Fact]
    public void Record_BlankDiagnosisText_ThrowsDiagnosisBlank()
    {
        var act = () => Diagnosis.Record(3, "", "J11", 0);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("diagnosis_blank");
    }

    [Fact]
    public void Deactivate_Complaint_MarksItInactive()
    {
        var complaint = Complaint.Reconstitute(1, 3, "Cough", 0, isActive: true, [1]);

        complaint.Deactivate();

        complaint.IsActive.Should().BeFalse();
    }
}
