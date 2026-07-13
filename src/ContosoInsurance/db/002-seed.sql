USE ContosoInsurance;
GO

-- Users (SHA1 hashes of 'Password1' + salt — deliberately weak)
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'agent1')
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash, Salt, Role) VALUES
        (N'agent1',   N'7c222fb2927d828af22f592134e8932480637c0d', N'salt-agent1',   N'Agent'),
        (N'adjuster', N'6c85b0e3b7be5b6b8d7be2b6d1c1a2f3e4d5c6b7', N'salt-adj',      N'Adjuster'),
        (N'admin',    N'2fd4e1c67a2d28fced849ee1bb76e7391b93eb12', N'salt-admin',   N'Admin');
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
