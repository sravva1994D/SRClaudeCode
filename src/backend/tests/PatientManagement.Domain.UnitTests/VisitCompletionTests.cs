using FluentAssertions;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Visits;

namespace PatientManagement.Domain.UnitTests;

public sealed class VisitCompletionTests
{
    [Fact]
    public void Complete_InProgressVisit_BecomesCompleted()
    {
        var visit = Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]);

        visit.Complete();

        visit.StatusId.Should().Be(VisitStatuses.Completed);
    }

    [Fact]
    public void Complete_CompletedVisit_ThrowsNotInProgress()
    {
        var visit = Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]);

        var act = () => visit.Complete();

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("visit_not_in_progress");
    }
}
