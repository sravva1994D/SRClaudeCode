-- Prescriptions: the printable prescription issued for a completed visit (plan Sprint 9, item 40). One active prescription
-- per visit (filtered unique index). The content is built from the visit when it is viewed; item 41 adds the print snapshot.

CREATE TABLE dbo.Prescriptions (
    PrescriptionId int IDENTITY(1,1) NOT NULL,
    VisitId int NOT NULL,
    PrescriptionDate date NOT NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Prescriptions_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_Prescriptions_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_Prescriptions_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Prescriptions PRIMARY KEY (PrescriptionId),
    CONSTRAINT FK_Prescriptions_Visits_VisitId FOREIGN KEY (VisitId) REFERENCES dbo.Visits (VisitId),
    CONSTRAINT FK_Prescriptions_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Prescriptions_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);

CREATE UNIQUE NONCLUSTERED INDEX UX_Prescriptions_VisitId
    ON dbo.Prescriptions (VisitId)
    WHERE IsActive = 1;
