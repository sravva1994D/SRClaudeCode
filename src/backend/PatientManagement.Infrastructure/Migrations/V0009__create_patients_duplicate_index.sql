-- Supports the duplicate warning: a lookup by date of birth, then by name (plan Sprint 3, item 20).
-- Filtered to active patients, because the duplicate check never considers deactivated records.

CREATE NONCLUSTERED INDEX IX_Patients_DateOfBirth_FullName
    ON dbo.Patients (DateOfBirth, FullName)
    WHERE IsActive = 1;
