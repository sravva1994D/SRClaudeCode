-- Prescription prints (plan Sprint 9, item 41). Every print stores a snapshot of the prescription content as it was at
-- print time (BRD snapshot exception), so a reprint shows what was printed, not the current records.

CREATE TABLE dbo.PrescriptionPrints (
    PrescriptionPrintId int IDENTITY(1,1) NOT NULL,
    PrescriptionId int NOT NULL,
    PrintedAt datetime2(3) NOT NULL,
    SnapshotJson nvarchar(max) NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_PrescriptionPrints_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_PrescriptionPrints_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_PrescriptionPrints_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_PrescriptionPrints PRIMARY KEY (PrescriptionPrintId),
    CONSTRAINT FK_PrescriptionPrints_Prescriptions_PrescriptionId FOREIGN KEY (PrescriptionId) REFERENCES dbo.Prescriptions (PrescriptionId),
    CONSTRAINT FK_PrescriptionPrints_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_PrescriptionPrints_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);

CREATE NONCLUSTERED INDEX IX_PrescriptionPrints_PrescriptionId_PrintedAt
    ON dbo.PrescriptionPrints (PrescriptionId, PrintedAt);
