-- The audit log is append-only (CLAUDE.md 6.4). Updates and deletes are refused for every login, including the
-- owner's. Production also grants the application login only INSERT and SELECT on this table (CLAUDE.md 10.7).
CREATE TRIGGER dbo.TR_AuditLogs_AppendOnly
ON dbo.AuditLogs
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    THROW 50000, N'AuditLogs is append-only.', 1;
END;
