using PatientManagement.Domain.Prescriptions;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads and records prescription prints. A print is recorded inside a unit of work and is not audited (Q17).</summary>
public interface IPrescriptionPrintRepository
{
    /// <summary>Lists the prints of a prescription, oldest first. Snapshots are read by <see cref="FindAsync"/> only.</summary>
    Task<IReadOnlyList<PrescriptionPrint>> ListForPrescriptionAsync(int prescriptionId, CancellationToken cancellationToken);

    /// <summary>Returns one print of the prescription, or null when it does not belong to it.</summary>
    Task<PrescriptionPrint?> FindAsync(int prescriptionId, int prescriptionPrintId, CancellationToken cancellationToken);

    /// <summary>Inserts the print and sets its id.</summary>
    Task AddAsync(PrescriptionPrint print, int changedByUserId, CancellationToken cancellationToken);
}
