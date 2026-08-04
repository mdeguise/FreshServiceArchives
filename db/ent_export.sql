/*
    Separate archive for the ENT (Enterprise) groups:
        13000280196  ENT - Accounts Payable
        13000276221  ENT - Procurement
        13000276177  ENT - Solution Architecture

    Creates standalone snapshot tables (tickets, conversations, attachments) holding
    only these groups, plus repoints the attachment LocalFilePath at the separate
    ENT_Attachements folder (files are copied there separately). Originals untouched.
    Idempotent: drops and recreates the FS_ENT_* tables on each run.
*/

USE [FreshserviceArchive];
GO

DECLARE @g TABLE (GroupID BIGINT PRIMARY KEY);
INSERT INTO @g VALUES (13000280196), (13000276221), (13000276177);

-- Tickets -------------------------------------------------------------------
IF OBJECT_ID('dbo.FS_ENT_Tickets_Archive', 'U') IS NOT NULL DROP TABLE dbo.FS_ENT_Tickets_Archive;
SELECT t.*
INTO dbo.FS_ENT_Tickets_Archive
FROM dbo.FS_Tickets_Archive t
WHERE t.GroupID IN (SELECT GroupID FROM @g);
ALTER TABLE dbo.FS_ENT_Tickets_Archive ADD CONSTRAINT PK_FS_ENT_Tickets_Archive PRIMARY KEY (TicketID);

-- Conversations -------------------------------------------------------------
IF OBJECT_ID('dbo.FS_ENT_Ticket_Conversations_Archive', 'U') IS NOT NULL DROP TABLE dbo.FS_ENT_Ticket_Conversations_Archive;
SELECT c.*
INTO dbo.FS_ENT_Ticket_Conversations_Archive
FROM dbo.FS_Ticket_Conversations_Archive c
WHERE c.TicketID IN (SELECT TicketID FROM dbo.FS_ENT_Tickets_Archive);

-- Attachments (metadata), with LocalFilePath repointed at the ENT folder ------
IF OBJECT_ID('dbo.FS_ENT_Ticket_Attachments_Archive', 'U') IS NOT NULL DROP TABLE dbo.FS_ENT_Ticket_Attachments_Archive;
SELECT a.*
INTO dbo.FS_ENT_Ticket_Attachments_Archive
FROM dbo.FS_Ticket_Attachments_Archive a
WHERE a.TicketID IN (SELECT TicketID FROM dbo.FS_ENT_Tickets_Archive);

UPDATE dbo.FS_ENT_Ticket_Attachments_Archive
SET LocalFilePath = REPLACE(LocalFilePath, '\Attachements\', '\ENT_Attachements\')
WHERE LocalFilePath IS NOT NULL;
GO

-- SELECT INTO copies columns/nullability but NOT default constraints. The archiver's
-- INSERT relies on defaults for ImportedAt/LastSyncedAt/Downloaded, so re-add them to
-- match the source tables (otherwise inserts fail: "Cannot insert NULL into LastSyncedAt").
ALTER TABLE dbo.FS_ENT_Tickets_Archive              ADD CONSTRAINT DF_FS_ENT_Tickets_ImportedAt   DEFAULT SYSUTCDATETIME() FOR ImportedAt;
ALTER TABLE dbo.FS_ENT_Tickets_Archive              ADD CONSTRAINT DF_FS_ENT_Tickets_LastSyncedAt DEFAULT SYSUTCDATETIME() FOR LastSyncedAt;
ALTER TABLE dbo.FS_ENT_Ticket_Conversations_Archive ADD CONSTRAINT DF_FS_ENT_Conv_ImportedAt      DEFAULT SYSUTCDATETIME() FOR ImportedAt;
ALTER TABLE dbo.FS_ENT_Ticket_Conversations_Archive ADD CONSTRAINT DF_FS_ENT_Conv_LastSyncedAt    DEFAULT SYSUTCDATETIME() FOR LastSyncedAt;
ALTER TABLE dbo.FS_ENT_Ticket_Attachments_Archive   ADD CONSTRAINT DF_FS_ENT_Att_ImportedAt       DEFAULT SYSUTCDATETIME() FOR ImportedAt;
ALTER TABLE dbo.FS_ENT_Ticket_Attachments_Archive   ADD CONSTRAINT DF_FS_ENT_Att_Downloaded       DEFAULT ((0))            FOR Downloaded;
GO

-- Summary -------------------------------------------------------------------
SELECT 'Tickets='       + CONVERT(varchar, COUNT(*)) FROM dbo.FS_ENT_Tickets_Archive;
SELECT 'Conversations=' + CONVERT(varchar, COUNT(*)) FROM dbo.FS_ENT_Ticket_Conversations_Archive;
SELECT 'Attachments='   + CONVERT(varchar, COUNT(*)) FROM dbo.FS_ENT_Ticket_Attachments_Archive;
GO
