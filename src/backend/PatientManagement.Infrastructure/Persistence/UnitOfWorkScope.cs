using System.Data.Common;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>
/// The connection and transaction of the unit of work running on the current async flow. Repositories and the audit
/// writer read it so that every write joins the same transaction.
/// </summary>
internal sealed class UnitOfWorkScope(DbConnection connection, DbTransaction transaction)
{
    private static readonly AsyncLocal<UnitOfWorkScope?> Ambient = new();

    public DbConnection Connection { get; } = connection;

    public DbTransaction Transaction { get; } = transaction;

    /// <summary>The scope of the current unit of work. Writes outside a unit of work are refused.</summary>
    public static UnitOfWorkScope Current =>
        Ambient.Value ?? throw new InvalidOperationException("Database writes must run inside a unit of work.");

    public static UnitOfWorkScope? TryGetCurrent() => Ambient.Value;

    internal static void Enter(UnitOfWorkScope scope) => Ambient.Value = scope;

    internal static void Exit() => Ambient.Value = null;
}
