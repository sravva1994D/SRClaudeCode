-- Supports the patient list and search (plan Sprint 3, item 21). Both indexes cover active patients only, because the
-- list never shows deactivated records. Name search uses a leading wildcard, so the name index narrows the scan only
-- by collation order; the 2-second target at 10,000 patients is checked with the synthetic data set in Sprint 11.

CREATE NONCLUSTERED INDEX IX_Patients_FullName
    ON dbo.Patients (FullName)
    INCLUDE (DateOfBirth, GenderId, PhoneNumber)
    WHERE IsActive = 1;

CREATE NONCLUSTERED INDEX IX_Patients_PhoneNumber
    ON dbo.Patients (PhoneNumber)
    WHERE IsActive = 1 AND PhoneNumber IS NOT NULL;
