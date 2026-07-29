/*
    Access-control table for the Freshservice Archive portal.

    The RH (HR) groups are restricted: only users listed here may see tickets,
    groups, and attachments belonging to those groups. Everyone who can reach the
    site can see the non-RH groups.

    WHICH groups are restricted is configured in appsettings.json
    (Access:RestrictedGroupIds). WHO may see them is this table — edit it freely
    (SSMS or any tool); changes take effect immediately, no redeploy.

    UserName must be the Windows sign-in name (sAMAccountName), e.g. 'mdeguise'
    — NOT the email. Matching is case-insensitive.
*/

IF OBJECT_ID('dbo.FS_Portal_RH_Access', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.FS_Portal_RH_Access
    (
        UserName    NVARCHAR(128) NOT NULL CONSTRAINT PK_FS_Portal_RH_Access PRIMARY KEY,
        Email       NVARCHAR(256) NULL,
        DisplayName NVARCHAR(256) NULL,
        AddedAtUtc  DATETIME2(0)  NOT NULL CONSTRAINT DF_FS_Portal_RH_Access_AddedAt DEFAULT SYSUTCDATETIME(),
        AddedBy     NVARCHAR(128) NULL
    );
END
GO

-- Seed the three approved RH viewers (idempotent).
MERGE dbo.FS_Portal_RH_Access AS t
USING (VALUES
    ('mdeguise', 'mdeguise@tremblant.ca', 'Marc DeGuise'),
    ('scontant', 'scontant@tremblant.ca', NULL),
    ('guyd',     'guyd@tremblant.ca',     NULL)
) AS s(UserName, Email, DisplayName)
ON LOWER(t.UserName) = LOWER(s.UserName)
WHEN NOT MATCHED THEN
    INSERT (UserName, Email, DisplayName, AddedBy)
    VALUES (s.UserName, s.Email, s.DisplayName, 'seed');
GO

SELECT UserName, Email, DisplayName, AddedAtUtc FROM dbo.FS_Portal_RH_Access ORDER BY UserName;
GO
