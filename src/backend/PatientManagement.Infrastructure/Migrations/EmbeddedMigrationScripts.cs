using System.Reflection;
using System.Text.RegularExpressions;

namespace PatientManagement.Infrastructure.Migrations;

/// <summary>Reads the versioned scripts compiled into an assembly, in version order.</summary>
public static class EmbeddedMigrationScripts
{
    private const string ResourcePrefix = "Migrations/";
    private const string ScriptExtension = ".sql";

    // Fixed-width version numbers make ordinal name order equal version order.
    private static readonly Regex ScriptNamePattern = new(@"^V\d{4}__[a-z0-9_]+$", RegexOptions.CultureInvariant);

    public static IReadOnlyList<MigrationScript> Load(Assembly assembly)
    {
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal)
                && name.EndsWith(ScriptExtension, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

        var scripts = new List<MigrationScript>();
        foreach (var resourceName in resourceNames)
        {
            var name = resourceName[ResourcePrefix.Length..^ScriptExtension.Length];
            if (!ScriptNamePattern.IsMatch(name))
            {
                throw new MigrationException($"Migration file name '{name}' must match V<NNNN>__<snake_case>.");
            }

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new MigrationException($"Migration '{name}' could not be read from the assembly.");
            using var reader = new StreamReader(stream);
            scripts.Add(new MigrationScript(name, reader.ReadToEnd()));
        }

        var duplicateVersion = scripts.GroupBy(script => script.Name[..5]).FirstOrDefault(group => group.Count() > 1);
        if (duplicateVersion is not null)
        {
            throw new MigrationException($"More than one migration uses version {duplicateVersion.Key}.");
        }

        return scripts;
    }
}
