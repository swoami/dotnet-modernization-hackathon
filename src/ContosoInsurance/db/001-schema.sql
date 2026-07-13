-- ContosoInsurance schema (legacy)
IF DB_ID('ContosoInsurance') IS NULL
    CREATE DATABASE ContosoInsurance;
GO
USE ContosoInsurance;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserId       INT IDENTITY(1,1) PRIMARY KEY,
        Username     NVARCHAR(64)  NOT NULL UNIQUE,
        PasswordHash NVARCHAR(128) NOT NULL,
        Salt         NVARCHAR(64)  NOT NULL,
        Role         NVARCHAR(32)  NOT NULL DEFAULT 'Agent'
    );
END
GO

IF OBJECT_ID('dbo.Policies', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Policies (
        PolicyId       INT IDENTITY(1,1) PRIMARY KEY,
        PolicyNumber   NVARCHAR(32)  NOT NULL UNIQUE,
        HolderName     NVARCHAR(128) NOT NULL,
        ProductLine    NVARCHAR(32)  NOT NULL,
        CoverageAmount DECIMAL(18,2) NOT NULL,
        EffectiveDate  DATETIME2     NOT NULL,
        ExpirationDate DATETIME2     NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Claims', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Claims (
        ClaimId       INT IDENTITY(1,1) PRIMARY KEY,
        PolicyId      INT NOT NULL REFERENCES dbo.Policies(PolicyId),
        ClaimantName  NVARCHAR(128) NOT NULL,
        Amount        DECIMAL(18,2) NOT NULL,
        Status        NVARCHAR(32)  NOT NULL DEFAULT 'Pending',
        FiledOn       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        ClosedOn      DATETIME2     NULL,
        DocumentPath  NVARCHAR(512) NULL,
        Score         INT NULL,
        Notes         NVARCHAR(1024) NULL
    );
    CREATE INDEX IX_Claims_FiledOn ON dbo.Claims(FiledOn DESC);
END
GO

IF OBJECT_ID('dbo.ExportLog', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExportLog (
        ExportId    INT IDENTITY(1,1) PRIMARY KEY,
        ExportedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        FilePath    NVARCHAR(512) NOT NULL,
        RowCount    INT NOT NULL
    );
END
GO
