-- Patient master record (BRD: Patient Management). Patients are deactivated (IsActive = 0), never deleted (CLAUDE.md 6.5).
-- PhoneNumber holds digits only (CLAUDE.md 6.6); the client applies the display format.
-- Search indexes are added with the search item in Sprint 3 (plan item 21).

CREATE TABLE dbo.Patients (
    PatientId int IDENTITY(1,1) NOT NULL,
    FullName nvarchar(200) NOT NULL,
    DateOfBirth date NOT NULL,
    GenderId int NOT NULL,
    PhoneNumber nvarchar(20) NULL,
    Address nvarchar(500) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Patients_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_Patients_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_Patients_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Patients PRIMARY KEY (PatientId),
    CONSTRAINT CK_Patients_FullName CHECK (LEN(LTRIM(RTRIM(FullName))) > 0),
    CONSTRAINT FK_Patients_Genders_GenderId FOREIGN KEY (GenderId) REFERENCES dbo.Genders (GenderId),
    CONSTRAINT FK_Patients_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Patients_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);
