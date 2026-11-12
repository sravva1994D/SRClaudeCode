using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Domain.Consultation;

/// <summary>The vital signs the consultation records. The names match the VitalSign values in dbo.VitalRanges.</summary>
public static class VitalSigns
{
    public const string Temperature = "Temperature";
    public const string BloodPressureSystolic = "BloodPressureSystolic";
    public const string BloodPressureDiastolic = "BloodPressureDiastolic";
    public const string Pulse = "Pulse";
}

/// <summary>The configured bounds for one vital sign (plan Sprint 7–8, item 33; Q7). Outside normal = warn; outside plausible = block.</summary>
public sealed record VitalRange(
    string VitalSign,
    decimal NormalMinimum,
    decimal NormalMaximum,
    decimal PlausibleMinimum,
    decimal PlausibleMaximum);

/// <summary>How one value compares with its configured range.</summary>
public enum VitalAssessment
{
    /// <summary>No range is configured for this sign, so nothing is checked.</summary>
    NotChecked,

    /// <summary>Inside the normal range.</summary>
    Normal,

    /// <summary>Outside the normal range but plausible: the doctor is warned and may still save.</summary>
    Abnormal,

    /// <summary>Outside the plausible range: the value is impossible and cannot be saved.</summary>
    Implausible,
}

public static class VitalAssessor
{
    public static VitalAssessment Assess(decimal value, VitalRange? range)
    {
        if (range is null)
        {
            return VitalAssessment.NotChecked;
        }

        if (value < range.PlausibleMinimum || value > range.PlausibleMaximum)
        {
            return VitalAssessment.Implausible;
        }

        return value < range.NormalMinimum || value > range.NormalMaximum
            ? VitalAssessment.Abnormal
            : VitalAssessment.Normal;
    }
}

/// <summary>The vitals recorded for one visit. Values are optional; only the values that were measured are stored.</summary>
public sealed class Vitals
{
    private Vitals(
        int vitalsId,
        int visitId,
        decimal? temperature,
        int? bloodPressureSystolic,
        int? bloodPressureDiastolic,
        int? pulse,
        bool isActive,
        byte[] rowVersion)
    {
        VitalsId = vitalsId;
        VisitId = visitId;
        Temperature = temperature;
        BloodPressureSystolic = bloodPressureSystolic;
        BloodPressureDiastolic = bloodPressureDiastolic;
        Pulse = pulse;
        IsActive = isActive;
        RowVersion = rowVersion;
    }

    public int VitalsId { get; private set; }

    public int VisitId { get; private set; }

    public decimal? Temperature { get; private set; }

    public int? BloodPressureSystolic { get; private set; }

    public int? BloodPressureDiastolic { get; private set; }

    public int? Pulse { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; }

    /// <summary>
    /// Records the vitals for a visit. Each value is checked against its range: an impossible value throws
    /// <see cref="VitalsImplausibleException"/> and nothing is recorded; abnormal values are returned as warnings.
    /// </summary>
    public static Vitals Record(
        int visitId,
        decimal? temperature,
        int? bloodPressureSystolic,
        int? bloodPressureDiastolic,
        int? pulse,
        IReadOnlyDictionary<string, VitalRange> ranges,
        out IReadOnlyList<string> warnings)
    {
        warnings = CheckValues(temperature, bloodPressureSystolic, bloodPressureDiastolic, pulse, ranges);
        return new Vitals(0, visitId, temperature, bloodPressureSystolic, bloodPressureDiastolic, pulse, isActive: true, Array.Empty<byte>());
    }

    /// <summary>
    /// Replaces the recorded values with a correction (plan item 38). The same range rules apply as when recording.
    /// Returns true when any value changed, so an unchanged correction writes nothing.
    /// </summary>
    public bool Revise(
        decimal? temperature,
        int? bloodPressureSystolic,
        int? bloodPressureDiastolic,
        int? pulse,
        IReadOnlyDictionary<string, VitalRange> ranges,
        out IReadOnlyList<string> warnings)
    {
        warnings = CheckValues(temperature, bloodPressureSystolic, bloodPressureDiastolic, pulse, ranges);

        var changed = Temperature != temperature
            || BloodPressureSystolic != bloodPressureSystolic
            || BloodPressureDiastolic != bloodPressureDiastolic
            || Pulse != pulse;
        Temperature = temperature;
        BloodPressureSystolic = bloodPressureSystolic;
        BloodPressureDiastolic = bloodPressureDiastolic;
        Pulse = pulse;
        return changed;
    }

    /// <summary>Rebuilds vitals from a stored row.</summary>
    public static Vitals Reconstitute(
        int vitalsId,
        int visitId,
        decimal? temperature,
        int? bloodPressureSystolic,
        int? bloodPressureDiastolic,
        int? pulse,
        bool isActive,
        byte[] rowVersion) =>
        new(vitalsId, visitId, temperature, bloodPressureSystolic, bloodPressureDiastolic, pulse, isActive, rowVersion);

    /// <summary>Called by the repository after insert, when the database has assigned the id.</summary>
    public void AssignVitalsId(int vitalsId) => VitalsId = vitalsId;

    /// <summary>Called by the repository after each save, with the new row version.</summary>
    public void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;

    /// <summary>Values written to the audit row.</summary>
    public IReadOnlyDictionary<string, object?> AuditValues() => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["VisitId"] = VisitId,
        ["Temperature"] = Temperature,
        ["BloodPressureSystolic"] = BloodPressureSystolic,
        ["BloodPressureDiastolic"] = BloodPressureDiastolic,
        ["Pulse"] = Pulse,
        ["IsActive"] = IsActive,
    };

    private static List<string> CheckValues(
        decimal? temperature,
        int? bloodPressureSystolic,
        int? bloodPressureDiastolic,
        int? pulse,
        IReadOnlyDictionary<string, VitalRange> ranges)
    {
        var implausible = new List<string>();
        var abnormal = new List<string>();

        Check(VitalSigns.Temperature, temperature, ranges, implausible, abnormal);
        Check(VitalSigns.BloodPressureSystolic, bloodPressureSystolic, ranges, implausible, abnormal);
        Check(VitalSigns.BloodPressureDiastolic, bloodPressureDiastolic, ranges, implausible, abnormal);
        Check(VitalSigns.Pulse, pulse, ranges, implausible, abnormal);

        if (implausible.Count > 0)
        {
            throw new VitalsImplausibleException(implausible);
        }

        return abnormal;
    }

    private static void Check(
        string sign,
        decimal? value,
        IReadOnlyDictionary<string, VitalRange> ranges,
        List<string> implausible,
        List<string> abnormal)
    {
        if (value is null)
        {
            return;
        }

        ranges.TryGetValue(sign, out var range);
        switch (VitalAssessor.Assess(value.Value, range))
        {
            case VitalAssessment.Implausible:
                implausible.Add(sign);
                break;
            case VitalAssessment.Abnormal:
                abnormal.Add(sign);
                break;
        }
    }
}
