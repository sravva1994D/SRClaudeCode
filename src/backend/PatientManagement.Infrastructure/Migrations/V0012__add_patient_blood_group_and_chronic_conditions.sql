-- Blood group (fixed list, seeded in V0002) and chronic conditions (free text in Phase 1) for each patient (plan
-- Sprint 3, item 23; decision Q3). Both are optional: a patient may be registered before these are recorded.
-- Adding nullable columns to Patients does not rewrite or change any existing row.

ALTER TABLE dbo.Patients
    ADD BloodGroupId int NULL
        CONSTRAINT FK_Patients_BloodGroups_BloodGroupId FOREIGN KEY REFERENCES dbo.BloodGroups (BloodGroupId),
        ChronicConditions nvarchar(1000) NULL;
