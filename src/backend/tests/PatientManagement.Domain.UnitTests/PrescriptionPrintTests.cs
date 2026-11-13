using FluentAssertions;
using PatientManagement.Domain.Prescriptions;

namespace PatientManagement.Domain.UnitTests;

public sealed class PrescriptionPrintTests
{
    private static readonly DateTime PrintedAt = new(2026, 11, 12, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Record_ValidSnapshot_KeepsTheTimeAndTheContentThatWasPrinted()
    {
        var print = PrescriptionPrint.Record(9, PrintedAt, "{\"medications\":[]}");

        print.PrescriptionId.Should().Be(9);
        print.PrintedAt.Should().Be(PrintedAt);
        print.SnapshotJson.Should().Be("{\"medications\":[]}");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_BlankSnapshot_Throws(string snapshot)
    {
        var act = () => PrescriptionPrint.Record(9, PrintedAt, snapshot);

        act.Should().Throw<ArgumentException>().WithParameterName("snapshotJson");
    }
}
