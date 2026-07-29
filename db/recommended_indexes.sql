/*
    Recommended indexes for the Freshservice Archive search portal.

    The archive tables themselves (FS_Tickets_Archive, FS_Ticket_Conversations_Archive,
    FS_Requesters_Archive, FS_Responders_Archive, FS_Groups_Archive,
    FS_Ticket_Attachments_Archive) are created and populated by the existing
    Freshservice archiver console app. This portal only READS them.

    These indexes are optional but make the list/search and detail queries fast on
    large archives. Run against the archive database. Safe to re-run (guarded).
*/

SET NOCOUNT ON;

-- List ordering / date filtering is almost always by UpdatedAt or CreatedAt.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FS_Tickets_Archive_UpdatedAt')
    CREATE NONCLUSTERED INDEX IX_FS_Tickets_Archive_UpdatedAt
        ON dbo.FS_Tickets_Archive (UpdatedAt DESC)
        INCLUDE (Subject, Status, Priority, GroupID, RequesterID, ResponderID, CreatedAt);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FS_Tickets_Archive_CreatedAt')
    CREATE NONCLUSTERED INDEX IX_FS_Tickets_Archive_CreatedAt
        ON dbo.FS_Tickets_Archive (CreatedAt DESC);

-- Filter dropdowns (Status / GroupID).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FS_Tickets_Archive_Status')
    CREATE NONCLUSTERED INDEX IX_FS_Tickets_Archive_Status
        ON dbo.FS_Tickets_Archive (Status);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FS_Tickets_Archive_GroupID')
    CREATE NONCLUSTERED INDEX IX_FS_Tickets_Archive_GroupID
        ON dbo.FS_Tickets_Archive (GroupID);

-- Detail page: conversations and attachments are looked up by TicketID.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FS_Conversations_TicketID')
    CREATE NONCLUSTERED INDEX IX_FS_Conversations_TicketID
        ON dbo.FS_Ticket_Conversations_Archive (TicketID, CreatedAt);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FS_Attachments_TicketID')
    CREATE NONCLUSTERED INDEX IX_FS_Attachments_TicketID
        ON dbo.FS_Ticket_Attachments_Archive (TicketID, ConversationID);
GO
