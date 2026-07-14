USE ContosoInsurance;
GO

-- Development-only login accounts: exactly one account for each currently seeded role.
-- All use Password1; values are ASP.NET Core Identity PasswordHasher<User> PBKDF2 hashes.
-- The update/insert below makes these fixed local-demo accounts idempotent.
DECLARE @DemoUsers TABLE
(
    Username NVARCHAR(64) NOT NULL PRIMARY KEY,
    PasswordHash NVARCHAR(128) NOT NULL,
    Role NVARCHAR(32) NOT NULL UNIQUE
);

INSERT INTO @DemoUsers (Username, PasswordHash, Role) VALUES
    (N'agent1', N'AQAAAAIAAYagAAAAEPzmJ4R+71ks71AOqvfd2tkoX3aGagDuIn8aeZJ6ZXsaH6TFpltZPXTGOj/FJhzCeA==', N'Agent'),
    (N'adjuster', N'AQAAAAIAAYagAAAAELKFMXNLmZgdGEjGZwBgAouCOcooNfzKn/ui4WEnIW3kaXct/aCPHsV83pDQN42bbQ==', N'Adjuster'),
    (N'admin', N'AQAAAAIAAYagAAAAEIfQzvaULCgTnRMpieA8XZkjqMaXEnizE7XhwCYp7sqsm7NLERP6cTWX+215/iJ8fA==', N'Admin');

UPDATE users
SET
    PasswordHash = demo.PasswordHash,
    Role = demo.Role
FROM dbo.Users AS users
INNER JOIN @DemoUsers AS demo ON demo.Username = users.Username;

IF COL_LENGTH('dbo.Users', 'Salt') IS NULL
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash, Role)
    SELECT demo.Username, demo.PasswordHash, demo.Role
    FROM @DemoUsers AS demo
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Users AS users
        WHERE users.Username = demo.Username);
END
ELSE
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash, Salt, Role)
    SELECT demo.Username, demo.PasswordHash, N'', demo.Role
    FROM @DemoUsers AS demo
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Users AS users
        WHERE users.Username = demo.Username);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Policies)
BEGIN
    INSERT INTO dbo.Policies (PolicyNumber, HolderName, ProductLine, CoverageAmount, EffectiveDate, ExpirationDate) VALUES
        (N'POL-1001', N'Alice Johnson', N'Auto',  25000.00, '2024-01-01', '2025-12-31'),
        (N'POL-1002', N'Bob Smith',     N'Home', 350000.00, '2023-06-01', '2026-05-31'),
        (N'POL-1003', N'Carol Diaz',    N'Auto',  30000.00, '2024-08-15', '2025-08-14'),
        (N'POL-1004', N'David Nguyen',  N'Life', 500000.00, '2022-01-10', '2032-01-09'),
        (N'POL-1005', N'Eve Patel',     N'Home', 275000.00, '2024-11-01', '2027-10-31');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Claims)
BEGIN
    INSERT INTO dbo.Claims (PolicyId, ClaimantName, Amount, Status, FiledOn, DocumentPath, Notes) VALUES
        (1, N'Alice Johnson', 4200.00,  N'Pending',  DATEADD(day,-3, SYSUTCDATETIME()), N'C:\ClaimsFiles\1\photo.jpg', N'Rear-end collision'),
        (2, N'Bob Smith',    18500.00,  N'Pending',  DATEADD(day,-7, SYSUTCDATETIME()), N'C:\ClaimsFiles\2\estimate.pdf', N'Water damage'),
        (3, N'Carol Diaz',    1200.00,  N'Approved', DATEADD(day,-40, SYSUTCDATETIME()), NULL, N'Windshield replacement'),
        (4, N'David Nguyen', 25000.00,  N'Pending',  DATEADD(day,-1, SYSUTCDATETIME()), NULL, NULL),
        (5, N'Eve Patel',     9800.00,  N'Rejected', DATEADD(day,-60, SYSUTCDATETIME()), NULL, N'Out of policy period');
END
GO
