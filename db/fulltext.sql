/*
    Full-text index for fast "word in description" search.

    LIKE '%word%' over 43k+ large description bodies is a full scan and times out.
    A full-text index makes CONTAINS(DescriptionText, ...) fast. CHANGE_TRACKING AUTO
    keeps it current as the archiver adds rows. Requires Full-Text Search to be
    installed (SERVERPROPERTY('IsFullTextInstalled') = 1).

    Idempotent: safe to re-run.
*/

IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'FS_Archive_FT')
    CREATE FULLTEXT CATALOG FS_Archive_FT;
GO

IF NOT EXISTS (SELECT 1 FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('dbo.FS_Tickets_Archive'))
BEGIN
    -- The PK index name is auto-generated; look it up rather than hard-code it.
    DECLARE @pk sysname = (
        SELECT name FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.FS_Tickets_Archive') AND is_primary_key = 1
    );
    DECLARE @sql nvarchar(max) =
        N'CREATE FULLTEXT INDEX ON dbo.FS_Tickets_Archive(DescriptionText) '
        + N'KEY INDEX ' + QUOTENAME(@pk) + N' ON FS_Archive_FT WITH CHANGE_TRACKING AUTO;';
    EXEC sp_executesql @sql;
END
GO
