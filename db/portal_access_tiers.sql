/*
    Tiered access tables for the portal. A user's visible groups = the union of the
    grants from every table they appear in:
        FS_Portal_RH_Access    -> RH groups   (Access:RhGroupIds)
        FS_Portal_IT_Access    -> IT groups   (Access:ItGroupIds)
        FS_Portal_Full_Access  -> ALL groups  (Access:AllowedGroupIds)  + portal admin
    A user in none of the tables sees nothing. Full Access members can manage all
    three lists in the app's Access Management section.

    UserName = Windows sign-in name (sAMAccountName), e.g. 'mdeguise'. Idempotent.
*/

-- IT access ------------------------------------------------------------------
IF OBJECT_ID('dbo.FS_Portal_IT_Access', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FS_Portal_IT_Access
    (
        UserName    NVARCHAR(128) NOT NULL CONSTRAINT PK_FS_Portal_IT_Access PRIMARY KEY,
        Email       NVARCHAR(256) NULL,
        DisplayName NVARCHAR(256) NULL,
        AddedAtUtc  DATETIME2(0)  NOT NULL CONSTRAINT DF_FS_Portal_IT_Access_AddedAt DEFAULT SYSUTCDATETIME(),
        AddedBy     NVARCHAR(128) NULL
    );
END
GO

-- Full access (+ portal admins) ----------------------------------------------
IF OBJECT_ID('dbo.FS_Portal_Full_Access', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FS_Portal_Full_Access
    (
        UserName    NVARCHAR(128) NOT NULL CONSTRAINT PK_FS_Portal_Full_Access PRIMARY KEY,
        Email       NVARCHAR(256) NULL,
        DisplayName NVARCHAR(256) NULL,
        AddedAtUtc  DATETIME2(0)  NOT NULL CONSTRAINT DF_FS_Portal_Full_Access_AddedAt DEFAULT SYSUTCDATETIME(),
        AddedBy     NVARCHAR(128) NULL
    );
END
GO

-- Seed the initial admin / full-access user (needed to reach the admin section).
IF NOT EXISTS (SELECT 1 FROM dbo.FS_Portal_Full_Access WHERE LOWER(UserName) = 'mdeguise')
    INSERT INTO dbo.FS_Portal_Full_Access (UserName, Email, DisplayName, AddedBy)
    VALUES ('mdeguise', 'mdeguise@tremblant.ca', 'Marc DeGuise', 'seed');
GO

SELECT 'RH'   AS Tbl, UserName FROM dbo.FS_Portal_RH_Access
UNION ALL SELECT 'IT',   UserName FROM dbo.FS_Portal_IT_Access
UNION ALL SELECT 'FULL', UserName FROM dbo.FS_Portal_Full_Access
ORDER BY Tbl, UserName;
GO
