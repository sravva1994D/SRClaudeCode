-- Append-only record of data changes (CLAUDE.md 6.4, Q17). Every row stores the changed fields as JSON.
-- This table keeps its own ChangedDate instead of the six standard audit columns: an audit row is never updated,
-- so modified and created fields would only repeat it.
CREATE TABLE dbo.AuditLogs (
    AuditLogId bigint IDENTITY(1, 1) NOT NULL,
    TableName nvarchar(128) NOT NULL,
    RecordId nvarchar(64) NOT NULL,
    Action nvarchar(10) NOT NULL,
    OldValues nvarchar(max) NULL,
    NewValues nvarchar(max) NULL,
    ChangedDate datetime2(3) NOT NULL CONSTRAINT DF_AuditLogs_ChangedDate DEFAULT SYSUTCDATETIME(),
    UserId int NULL,
    CONSTRAINT PK_AuditLogs PRIMARY KEY (AuditLogId),
    CONSTRAINT CK_AuditLogs_Action CHECK (Action IN (N'Create', N'Update', N'Delete')),
    CONSTRAINT FK_AuditLogs_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT CK_AuditLogs_Values CHECK (
        (OldValues IS NULL OR ISJSON(OldValues) = 1)
        AND (NewValues IS NULL OR ISJSON(NewValues) = 1))
);

CREATE INDEX IX_AuditLogs_TableName_RecordId ON dbo.AuditLogs (TableName, RecordId);
