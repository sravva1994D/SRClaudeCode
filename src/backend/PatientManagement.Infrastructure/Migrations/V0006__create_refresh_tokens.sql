-- Refresh tokens (CLAUDE.md 10.1). Only a SHA-256 hash of each token is stored. A login starts a family: every
-- rotation keeps the family id, and ExpiresDate stays at the family's absolute limit (8 hours from login).
-- LastUsedDate drives the 30-minute idle limit.
CREATE TABLE dbo.RefreshTokens (
    RefreshTokenId int IDENTITY(1, 1) NOT NULL,
    UserId int NOT NULL,
    FamilyId uniqueidentifier NOT NULL,
    TokenHash char(64) NOT NULL,
    IssuedDate datetime2(3) NOT NULL,
    ExpiresDate datetime2(3) NOT NULL,
    LastUsedDate datetime2(3) NOT NULL,
    RevokedDate datetime2(3) NULL,
    RevokedReason nvarchar(50) NULL,
    IsActive bit NOT NULL CONSTRAINT DF_RefreshTokens_IsActive DEFAULT 1,
    CreatedDate datetime2(3) NOT NULL CONSTRAINT DF_RefreshTokens_CreatedDate DEFAULT SYSUTCDATETIME(),
    CreatedBy int NULL,
    ModifiedDate datetime2(3) NOT NULL CONSTRAINT DF_RefreshTokens_ModifiedDate DEFAULT SYSUTCDATETIME(),
    ModifiedBy int NULL,
    RowVersion rowversion NOT NULL,
    CONSTRAINT PK_RefreshTokens PRIMARY KEY (RefreshTokenId),
    CONSTRAINT UX_RefreshTokens_TokenHash UNIQUE (TokenHash),
    CONSTRAINT CK_RefreshTokens_ExpiresAfterIssued CHECK (ExpiresDate > IssuedDate),
    CONSTRAINT FK_RefreshTokens_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_RefreshTokens_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_RefreshTokens_Users_ModifiedBy FOREIGN KEY (ModifiedBy) REFERENCES dbo.Users (UserId)
);

CREATE INDEX IX_RefreshTokens_FamilyId ON dbo.RefreshTokens (FamilyId);
CREATE INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId);
