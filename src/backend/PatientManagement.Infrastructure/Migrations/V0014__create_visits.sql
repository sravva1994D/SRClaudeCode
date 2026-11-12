-- Visits: one consultation with a patient (plan Sprint 7–8, item 32). A visit can start from an appointment, so
-- AppointmentId is optional (0–1). A visit's lifecycle (InProgress, Completed) is separate from the appointment status.
-- The filtered unique index allows one active visit per appointment; a walk-in visit has no appointment.

CREATE TABLE dbo.VisitStatuses (
    VisitStatusId int NOT NULL,
    Name nvarchar(20) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_VisitStatuses_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_VisitStatuses_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_VisitStatuses_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_VisitStatuses PRIMARY KEY (VisitStatusId),
    CONSTRAINT UX_VisitStatuses_Name UNIQUE (Name)
);

INSERT INTO dbo.VisitStatuses (VisitStatusId, Name) VALUES (1, N'InProgress'), (2, N'Completed');

CREATE TABLE dbo.Visits (
    VisitId int IDENTITY(1,1) NOT NULL,
    PatientId int NOT NULL,
    AppointmentId int NULL,
    VisitStatusId int NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Visits_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_Visits_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_Visits_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Visits PRIMARY KEY (VisitId),
    CONSTRAINT FK_Visits_Patients_PatientId FOREIGN KEY (PatientId) REFERENCES dbo.Patients (PatientId),
    CONSTRAINT FK_Visits_Appointments_AppointmentId FOREIGN KEY (AppointmentId) REFERENCES dbo.Appointments (AppointmentId),
    CONSTRAINT FK_Visits_VisitStatuses_VisitStatusId FOREIGN KEY (VisitStatusId) REFERENCES dbo.VisitStatuses (VisitStatusId),
    CONSTRAINT FK_Visits_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Visits_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);

CREATE UNIQUE NONCLUSTERED INDEX UX_Visits_AppointmentId
    ON dbo.Visits (AppointmentId)
    WHERE AppointmentId IS NOT NULL AND IsActive = 1;

CREATE NONCLUSTERED INDEX IX_Visits_PatientId_CreatedDate
    ON dbo.Visits (PatientId, CreatedDate);
