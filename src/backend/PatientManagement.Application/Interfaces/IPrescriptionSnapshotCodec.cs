using PatientManagement.Application.Prescriptions;

namespace PatientManagement.Application.Interfaces;

/// <summary>Turns a printed prescription into the text stored as its snapshot, and back. The format is an infrastructure choice.</summary>
public interface IPrescriptionSnapshotCodec
{
    string Serialize(PrescriptionDocument document);

    PrescriptionDocument Deserialize(string snapshot);
}
