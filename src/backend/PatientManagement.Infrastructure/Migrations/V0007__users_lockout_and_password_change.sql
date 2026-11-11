-- Lockout window start (CLAUDE.md 10.2: 5 failures within 15 minutes) and the time the password last changed.
ALTER TABLE dbo.Users ADD
    FailedLoginWindowStartDate datetime2(3) NULL,
    PasswordChangedDate datetime2(3) NULL;
