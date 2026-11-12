-- Vitals recorded for a visit (plan Sprint 7–8, item 33): temperature, blood pressure and pulse. One vitals row per visit;
-- editing comes later (item 38). The normal and plausible bounds are read from dbo.VitalRanges, which is deliberately not
-- seeded until the doctor confirms them (Q7). Until then no warning or block applies.

CREATE TABLE dbo.Vitals (
    VitalsId int IDENTITY(1,1) NOT NULL,
    VisitId int NOT NULL,
    Temperature decimal(4, 1) NULL,
    BloodPressureSystolic int NULL,
    BloodPressureDiastolic int NULL,
    Pulse int NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Vitals_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_Vitals_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_Vitals_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Vitals PRIMARY KEY (VitalsId),
    CONSTRAINT FK_Vitals_Visits_VisitId FOREIGN KEY (VisitId) REFERENCES dbo.Visits (VisitId),
    CONSTRAINT FK_Vitals_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Vitals_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);

CREATE UNIQUE NONCLUSTERED INDEX UX_Vitals_VisitId
    ON dbo.Vitals (VisitId)
    WHERE IsActive = 1;
