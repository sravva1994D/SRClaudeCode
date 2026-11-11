using System.Security.Cryptography;
using System.Text;

namespace PatientManagement.Infrastructure.Migrations;

/// <summary>
/// One versioned schema script. It runs as a single batch inside a transaction, so it must not contain GO.
/// </summary>
public sealed record MigrationScript(string Name, string Sql)
{
    /// <summary>SHA-256 of the script text. An applied script whose checksum changes is refused.</summary>
    public string Checksum => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Sql)));
}
