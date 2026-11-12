using FluentAssertions;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Prescriptions;
using PatientManagement.Domain.Visits;

namespace PatientManagement.Domain.UnitTests;

public sealed class PrescriptionTests
{
    [Fact]
    public void Issue_NewVisit_IsActiveOnTheGivenDate()
    {
        var prescription = Prescription.Issue(3, new DateOnly(2026, 11, 12));

        prescription.VisitId.Should().Be(3);
        prescription.PrescriptionDate.Should().Be(new DateOnly(2026, 11, 12));
        prescription.IsActive.Should().BeTrue();
    }

    [Fact]
    public void AuditValues_HoldTheVisitDateAndStatus()
    {
        var prescription = Prescription.Issue(3, new DateOnly(2026, 11, 12));

        prescription.AuditValues()["PrescriptionDate"].Should().Be("2026-11-12");
        prescription.AuditValues()["VisitId"].Should().Be(3);
    }

    [Fact]
    public void EnsureCompleted_InProgressVisit_ThrowsNotCompleted()
    {
        var visit = Visit.Reconstitute(1, 7, 5, VisitStatuses.InProgress, true, [1]);

        var act = () => visit.EnsureCompleted();

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("visit_not_completed");
    }

    [Fact]
    public void EnsureCompleted_CompletedVisit_DoesNotThrow()
    {
        var visit = Visit.Reconstitute(1, 7, 5, VisitStatuses.Completed, true, [1]);

        var act = () => visit.EnsureCompleted();

        act.Should().NotThrow();
    }
}
