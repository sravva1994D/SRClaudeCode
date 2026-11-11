-- Reference data (plan: Genders, BloodGroups, AppointmentStatuses, VitalRanges). Primary keys are fixed
-- integers so application code and later migrations can refer to them without a lookup.
-- VitalRanges is created empty on purpose: the normal and plausible bounds must be confirmed by the doctor
-- (Q7, plan Sprint 7) before they are seeded.

CREATE TABLE dbo.Genders (
    GenderId int NOT NULL,
    Name nvarchar(50) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Genders_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_Genders_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_Genders_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Genders PRIMARY KEY (GenderId),
    CONSTRAINT UX_Genders_Name UNIQUE (Name),
    CONSTRAINT FK_Genders_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Genders_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);

CREATE TABLE dbo.BloodGroups (
    BloodGroupId int NOT NULL,
    Name nvarchar(20) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_BloodGroups_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_BloodGroups_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_BloodGroups_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_BloodGroups PRIMARY KEY (BloodGroupId),
    CONSTRAINT UX_BloodGroups_Name UNIQUE (Name),
    CONSTRAINT FK_BloodGroups_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_BloodGroups_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);

CREATE TABLE dbo.AppointmentStatuses (
    AppointmentStatusId int NOT NULL,
    Name nvarchar(20) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_AppointmentStatuses_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_AppointmentStatuses_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_AppointmentStatuses_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_AppointmentStatuses PRIMARY KEY (AppointmentStatusId),
    CONSTRAINT UX_AppointmentStatuses_Name UNIQUE (Name),
    CONSTRAINT FK_AppointmentStatuses_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_AppointmentStatuses_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);

CREATE TABLE dbo.VitalRanges (
    VitalRangeId int NOT NULL,
    VitalSign nvarchar(50) NOT NULL,
    Unit nvarchar(20) NOT NULL,
    NormalMinimum decimal(6, 2) NOT NULL,
    NormalMaximum decimal(6, 2) NOT NULL,
    PlausibleMinimum decimal(6, 2) NOT NULL,
    PlausibleMaximum decimal(6, 2) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_VitalRanges_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_VitalRanges_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_VitalRanges_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_VitalRanges PRIMARY KEY (VitalRangeId),
    CONSTRAINT UX_VitalRanges_VitalSign UNIQUE (VitalSign),
    -- The plausible range contains the normal range: values outside normal warn, values outside plausible block.
    CONSTRAINT CK_VitalRanges_Bounds CHECK (
        PlausibleMinimum <= NormalMinimum
        AND NormalMinimum <= NormalMaximum
        AND NormalMaximum <= PlausibleMaximum),
    CONSTRAINT FK_VitalRanges_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_VitalRanges_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);

INSERT INTO dbo.Genders (GenderId, Name) VALUES (1, N'Male'), (2, N'Female'), (3, N'Other');

INSERT INTO dbo.BloodGroups (BloodGroupId, Name) VALUES
    (1, N'A+'), (2, N'A−'), (3, N'B+'), (4, N'B−'), (5, N'AB+'), (6, N'AB−'), (7, N'O+'), (8, N'O−'), (9, N'Unknown');

INSERT INTO dbo.AppointmentStatuses (AppointmentStatusId, Name) VALUES
    (1, N'Scheduled'), (2, N'Completed'), (3, N'Cancelled'), (4, N'NoShow');
