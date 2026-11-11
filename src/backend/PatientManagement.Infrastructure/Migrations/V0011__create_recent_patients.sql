-- Recently opened patients for each user (plan Sprint 3, item 22). The application keeps at most 20 rows per user.
-- Opening a record is not audited (CLAUDE.md 10.9, Q17), so this table is a per-user convenience, not audit history.
-- It still carries the standard audit columns (CLAUDE.md 6.4); the user who opened the record is written to them.

CREATE TABLE dbo.RecentPatients (
    UserId int NOT NULL,
    PatientId int NOT NULL,
    LastViewedDate datetime2(3) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_RecentPatients_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_RecentPatients_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_RecentPatients_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_RecentPatients PRIMARY KEY (UserId, PatientId),
    CONSTRAINT FK_RecentPatients_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_RecentPatients_Patients_PatientId FOREIGN KEY (PatientId) REFERENCES dbo.Patients (PatientId),
    CONSTRAINT FK_RecentPatients_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_RecentPatients_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);

CREATE NONCLUSTERED INDEX IX_RecentPatients_UserId_LastViewedDate
    ON dbo.RecentPatients (UserId, LastViewedDate DESC);
