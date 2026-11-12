using FluentAssertions;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Visits;
using PatientManagement.Domain.Consultation;

namespace PatientManagement.Domain.UnitTests;

public sealed class VitalsTests
{
    private static readonly VitalRange Pulse = new(VitalSigns.Pulse, 60, 100, 20, 250);
    private static readonly IReadOnlyDictionary<string, VitalRange> Ranges =
        new Dictionary<string, VitalRange> { [VitalSigns.Pulse] = Pulse };
    private static readonly IReadOnlyDictionary<string, VitalRange> NoRanges =
        new Dictionary<string, VitalRange>();

    [Theory]
    [InlineData(72, VitalAssessment.Normal)]
    [InlineData(60, VitalAssessment.Normal)]
    [InlineData(120, VitalAssessment.Abnormal)]
    [InlineData(10, VitalAssessment.Implausible)]
    [InlineData(300, VitalAssessment.Implausible)]
    public void Assess_PulseValue_ClassifiesAgainstTheRange(decimal value, VitalAssessment expected)
    {
        VitalAssessor.Assess(value, Pulse).Should().Be(expected);
    }

    [Fact]
    public void Assess_NoConfiguredRange_IsNotChecked()
    {
        VitalAssessor.Assess(10, null).Should().Be(VitalAssessment.NotChecked);
    }

    [Fact]
    public void Record_AbnormalValue_IsSavedAndReturnedAsAWarning()
    {
        var vitals = Vitals.Record(3, null, null, null, 120, Ranges, out var warnings);

        vitals.Pulse.Should().Be(120);
        warnings.Should().Equal(VitalSigns.Pulse);
    }

    [Fact]
    public void Record_ImplausibleValue_ThrowsAndNamesTheSign()
    {
        var act = () => Vitals.Record(3, null, null, null, 10, Ranges, out _);

        act.Should().Throw<VitalsImplausibleException>()
            .Which.VitalSigns.Should().Equal(VitalSigns.Pulse);
    }

    [Fact]
    public void Record_NoConfiguredRanges_SavesWithoutWarnings()
    {
        var vitals = Vitals.Record(3, 36.6m, 120, 80, 10, NoRanges, out var warnings);

        vitals.Pulse.Should().Be(10);
        warnings.Should().BeEmpty();
    }

    [Fact]
    public void EnsureInProgress_CompletedVisit_ThrowsNotInProgress()
    {
        var visit = Visit.Reconstitute(1, 7, 5, VisitStatuses.Completed, true, [1]);

        var act = () => visit.EnsureInProgress();

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("visit_not_in_progress");
    }
}
