-- Doctor account (CLAUDE.md 10.1-10.2). Lockout state lives on the row so the 5-failures-in-15-minutes rule
-- survives restarts. Audit columns follow CLAUDE.md 6.4 on every table; CreatedBy and ModifiedBy stay NULL for
-- system rows (the bootstrap account is written by the seeding tool, which has no user to name).
CREATE TABLE dbo.Users (
    UserId int IDENTITY(1, 1) NOT NULL,
    Username nvarchar(64) NOT NULL,
    -- bcrypt output is 60 characters and embeds its own salt, so no separate salt column.
    PasswordHash nvarchar(100) NOT NULL,
    FailedLoginCount int NOT NULL CONSTRAINT DF_Users_FailedLoginCount DEFAULT 0,
    LockedUntilDate datetime2(3) NULL,
    LastLoginDate datetime2(3) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_Users_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_Users_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UX_Users_Username UNIQUE (Username),
    CONSTRAINT CK_Users_FailedLoginCount CHECK (FailedLoginCount >= 0),
    CONSTRAINT FK_Users_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Users_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);
