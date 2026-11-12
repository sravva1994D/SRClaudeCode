-- Complaints (symptoms) and diagnoses recorded during a visit (plan Sprint 7–8, item 34). A visit's notes are saved as a
-- draft while the visit is in progress: each save replaces the visit's active rows. A replaced row is deactivated and
-- kept, never deleted, so the audit trail stays complete. Text is limited to 2000 characters (BRD, Complaint and
-- Diagnosis). The CHECK constraints reject blank text as a last line of defence; the application drops blanks first.

CREATE TABLE dbo.Complaints (
    ComplaintId int IDENTITY(1,1) NOT NULL,
    VisitId int NOT NULL,
    ComplaintText nvarchar(2000) NOT NULL,
    SortOrder int NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Complaints_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_Complaints_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_Complaints_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Complaints PRIMARY KEY (ComplaintId),
    CONSTRAINT FK_Complaints_Visits_VisitId FOREIGN KEY (VisitId) REFERENCES dbo.Visits (VisitId),
    CONSTRAINT FK_Complaints_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Complaints_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Complaints_Text CHECK (LEN(LTRIM(RTRIM(ComplaintText))) > 0)
);

CREATE NONCLUSTERED INDEX IX_Complaints_VisitId_SortOrder
    ON dbo.Complaints (VisitId, SortOrder)
    WHERE IsActive = 1;

CREATE TABLE dbo.Diagnoses (
    DiagnosisId int IDENTITY(1,1) NOT NULL,
    VisitId int NOT NULL,
    DiagnosisText nvarchar(2000) NOT NULL,
    -- Optional reference code, kept as entered for the future ICD-10 integration (BRD, Diagnosis).
    DiagnosisCode nvarchar(20) NULL,
    SortOrder int NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Diagnoses_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_Diagnoses_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_Diagnoses_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Diagnoses PRIMARY KEY (DiagnosisId),
    CONSTRAINT FK_Diagnoses_Visits_VisitId FOREIGN KEY (VisitId) REFERENCES dbo.Visits (VisitId),
    CONSTRAINT FK_Diagnoses_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Diagnoses_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_Diagnoses_Text CHECK (LEN(LTRIM(RTRIM(DiagnosisText))) > 0)
);

CREATE NONCLUSTERED INDEX IX_Diagnoses_VisitId_SortOrder
    ON dbo.Diagnoses (VisitId, SortOrder)
    WHERE IsActive = 1;
