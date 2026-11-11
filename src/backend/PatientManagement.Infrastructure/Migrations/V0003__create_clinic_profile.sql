-- Prescription header and footer settings (Q4). One row only: the application reads and updates row 1.
-- Text fields are optional until the doctor fills them in, because prescriptions print whatever is set.
-- PhoneNumber holds digits only (CLAUDE.md 6.6); the client applies the display format.

CREATE TABLE dbo.ClinicProfile (
    ClinicProfileId int NOT NULL,
    ClinicName nvarchar(200) NULL,
    DoctorName nvarchar(200) NULL,
    Qualifications nvarchar(200) NULL,
    RegistrationNumber nvarchar(50) NULL,
    Address nvarchar(500) NULL,
    PhoneNumber nvarchar(20) NULL,
    FooterNotes nvarchar(1000) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_ClinicProfile_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_ClinicProfile_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_ClinicProfile_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_ClinicProfile PRIMARY KEY (ClinicProfileId),
    CONSTRAINT CK_ClinicProfile_SingleRow CHECK (ClinicProfileId = 1),
    CONSTRAINT FK_ClinicProfile_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_ClinicProfile_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);

INSERT INTO dbo.ClinicProfile (ClinicProfileId) VALUES (1);
