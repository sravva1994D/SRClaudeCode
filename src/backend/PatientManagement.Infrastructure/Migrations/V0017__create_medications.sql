-- Medicines prescribed during a visit (plan Sprint 7–8, item 35). Like complaints and diagnoses, a visit's medications are
-- saved as a draft while the visit is in progress: each save deactivates the active rows and inserts the new list. Rows
-- are never deleted, so the audit trail keeps old values.
--
-- Medicine names are free text; there is no drug master table (decision log, item 6). The suggestions come from the
-- doctor's own history, so the index on MedicineName serves the prefix search used by the autocomplete.

CREATE TABLE dbo.Medications (
    MedicationId int IDENTITY(1,1) NOT NULL,
    VisitId int NOT NULL,
    MedicineName nvarchar(200) NOT NULL,
    Dosage nvarchar(100) NOT NULL,
    Frequency nvarchar(100) NOT NULL,
    Duration nvarchar(100) NOT NULL,
    SpecialInstructions nvarchar(500) NULL,
    SortOrder int NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Medications_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_Medications_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_Medications_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Medications PRIMARY KEY (MedicationId),
    CONSTRAINT FK_Medications_Visits_VisitId FOREIGN KEY (VisitId) REFERENCES dbo.Visits (VisitId),
    CONSTRAINT FK_Medications_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Medications_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Medications_Name CHECK (LEN(LTRIM(RTRIM(MedicineName))) > 0),
    CONSTRAINT CK_Medications_Dosage CHECK (LEN(LTRIM(RTRIM(Dosage))) > 0),
    CONSTRAINT CK_Medications_Frequency CHECK (LEN(LTRIM(RTRIM(Frequency))) > 0),
    CONSTRAINT CK_Medications_Duration CHECK (LEN(LTRIM(RTRIM(Duration))) > 0)
);

CREATE NONCLUSTERED INDEX IX_Medications_VisitId_SortOrder
    ON dbo.Medications (VisitId, SortOrder)
    WHERE IsActive = 1;

CREATE NONCLUSTERED INDEX IX_Medications_MedicineName
    ON dbo.Medications (MedicineName)
    WHERE IsActive = 1;
