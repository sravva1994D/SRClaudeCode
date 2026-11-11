-- Appointments (plan Sprint 5–6, item 25). Slots are fixed 15-minute periods, stored as the UTC start of the slot.
-- A slot can hold one appointment. The filtered unique index enforces that in the database, so two requests that race
-- for the same slot cannot both succeed. Cancelled appointments (status 3) free their slot.

CREATE TABLE dbo.Appointments (
    AppointmentId int IDENTITY(1,1) NOT NULL,
    PatientId int NOT NULL,
    SlotStart datetime2(3) NOT NULL,
    AppointmentStatusId int NOT NULL CONSTRAINT DF_Appointments_AppointmentStatusId DEFAULT 1,
    IsActive bit NOT NULL CONSTRAINT DF_Appointments_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_Appointments_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_Appointments_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Appointments PRIMARY KEY (AppointmentId),
    CONSTRAINT FK_Appointments_Patients_PatientId FOREIGN KEY (PatientId) REFERENCES dbo.Patients (PatientId),
    CONSTRAINT FK_Appointments_AppointmentStatuses_AppointmentStatusId
        FOREIGN KEY (AppointmentStatusId) REFERENCES dbo.AppointmentStatuses (AppointmentStatusId),
    CONSTRAINT FK_Appointments_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Appointments_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);

CREATE UNIQUE NONCLUSTERED INDEX UX_Appointments_SlotStart
    ON dbo.Appointments (SlotStart)
    WHERE IsActive = 1 AND AppointmentStatusId <> 3;

CREATE NONCLUSTERED INDEX IX_Appointments_PatientId_SlotStart
    ON dbo.Appointments (PatientId, SlotStart);
