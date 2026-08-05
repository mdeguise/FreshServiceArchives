using System.Text;
using Dapper;
using FreshServiceArchives.Api.Models;
using Microsoft.Extensions.Options;

namespace FreshServiceArchives.Api.Data;

public sealed class TicketSearchQuery
{
    public string? Search { get; set; }
    public string? Requester { get; set; }
    public string? Description { get; set; }
    public int? Status { get; set; }
    public int? Priority { get; set; }
    public long? GroupId { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDesc { get; set; } = true;

    /// <summary>Groups the caller is permitted to see. Results are always constrained to these.</summary>
    public IReadOnlyList<long> AllowedGroupIds { get; set; } = Array.Empty<long>();
}

public interface ITicketRepository
{
    Task<PagedResult<TicketListItem>> SearchAsync(TicketSearchQuery query, CancellationToken ct);
    Task<TicketDetail?> GetByIdAsync(long ticketId, IReadOnlyList<long> allowedGroupIds, CancellationToken ct);
    Task<IReadOnlyList<GroupLookup>> GetGroupsAsync(IReadOnlyList<long> allowedGroupIds, CancellationToken ct);
    Task<AttachmentFile?> GetAttachmentFileAsync(long attachmentId, IReadOnlyList<long> allowedGroupIds, CancellationToken ct);
}

/// <summary>Metadata needed to stream an archived attachment from disk.</summary>
public sealed record AttachmentFile(string LocalFilePath, string FileName, string? ContentType);

public sealed class TicketRepository : ITicketRepository
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ArchiveOptions _archive;

    // Whitelist of sortable columns to avoid SQL injection via the SortBy field.
    private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TicketID"] = "t.TicketID",
        ["Subject"] = "t.Subject",
        ["Status"] = "t.Status",
        ["Priority"] = "t.Priority",
        ["CreatedAt"] = "t.CreatedAt",
        ["UpdatedAt"] = "t.UpdatedAt",
    };

    public TicketRepository(ISqlConnectionFactory factory, IOptions<ArchiveOptions> archive)
    {
        _factory = factory;
        _archive = archive.Value;
    }

    public async Task<PagedResult<TicketListItem>> SearchAsync(TicketSearchQuery query, CancellationToken ct)
    {
        var where = new StringBuilder(" WHERE t.GroupID IN @AllowedGroups ");
        var p = new DynamicParameters();
        p.Add("@AllowedGroups", query.AllowedGroupIds);

        // General search: ticket number or subject.
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            if (long.TryParse(term, out var idTerm))
            {
                where.Append(" AND (t.TicketID = @IdTerm OR t.Subject LIKE @Like) ");
                p.Add("@IdTerm", idTerm);
            }
            else
            {
                where.Append(" AND t.Subject LIKE @Like ");
            }
            p.Add("@Like", "%" + term + "%");
        }

        // Dedicated requester search: name or email.
        if (!string.IsNullOrWhiteSpace(query.Requester))
        {
            where.Append(@" AND (rq.FirstName LIKE @ReqLike OR rq.LastName LIKE @ReqLike
                OR rq.PrimaryEmail LIKE @ReqLike
                OR LTRIM(RTRIM(ISNULL(rq.FirstName,'') + ' ' + ISNULL(rq.LastName,''))) LIKE @ReqLike) ");
            p.Add("@ReqLike", "%" + query.Requester.Trim() + "%");
        }

        // Dedicated keyword-in-description search via full-text CONTAINS (fast, whole-word
        // with prefix). LIKE '%word%' full-scans the large body column and times out.
        var ftsTerm = BuildContainsTerm(query.Description);
        if (ftsTerm is not null)
        {
            where.Append(" AND CONTAINS(t.DescriptionText, @DescFts) ");
            p.Add("@DescFts", ftsTerm);
        }

        if (query.Status is not null) { where.Append(" AND t.Status = @Status "); p.Add("@Status", query.Status); }
        if (query.Priority is not null) { where.Append(" AND t.Priority = @Priority "); p.Add("@Priority", query.Priority); }
        if (query.GroupId is not null) { where.Append(" AND t.GroupID = @GroupId "); p.Add("@GroupId", query.GroupId); }
        if (query.CreatedFrom is not null) { where.Append(" AND t.CreatedAt >= @CreatedFrom "); p.Add("@CreatedFrom", query.CreatedFrom); }
        if (query.CreatedTo is not null) { where.Append(" AND t.CreatedAt < @CreatedTo "); p.Add("@CreatedTo", query.CreatedTo); }

        var sortCol = SortColumns.TryGetValue(query.SortBy, out var c) ? c : "t.CreatedAt";
        var sortDir = query.SortDesc ? "DESC" : "ASC";

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        p.Add("@Offset", (page - 1) * pageSize);
        p.Add("@PageSize", pageSize);

        var fromJoins = @"
            FROM dbo.FS_Tickets_Archive t
            LEFT JOIN dbo.FS_Requesters_Archive rq ON rq.RequesterID = t.RequesterID
            LEFT JOIN dbo.FS_Responders_Archive rs ON rs.ResponderID = t.ResponderID
            LEFT JOIN dbo.FS_Groups_Archive g ON g.GroupID = t.GroupID ";

        var listSql = $@"
            SELECT
                t.TicketID, t.Subject, t.Status, t.Priority,
                t.GroupID, g.Name AS GroupName,
                t.RequesterID,
                LTRIM(RTRIM(ISNULL(rq.FirstName,'') + ' ' + ISNULL(rq.LastName,''))) AS RequesterName,
                rq.PrimaryEmail AS RequesterEmail,
                t.ResponderID,
                LTRIM(RTRIM(ISNULL(rs.FirstName,'') + ' ' + ISNULL(rs.LastName,''))) AS ResponderName,
                t.CreatedAt, t.UpdatedAt,
                CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.FS_Ticket_Attachments_Archive a WHERE a.TicketID = t.TicketID)
                         THEN 1 ELSE 0 END AS bit) AS HasAttachments
            {fromJoins}
            {where}
            ORDER BY {sortCol} {sortDir}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT_BIG(1) {fromJoins} {where};";

        using var conn = _factory.Create();
        using var multi = await conn.QueryMultipleAsync(
            new CommandDefinition(listSql, p, cancellationToken: ct));

        var items = (await multi.ReadAsync<TicketListItem>()).AsList();
        var total = await multi.ReadSingleAsync<long>();

        foreach (var item in items)
        {
            item.StatusText = FreshserviceLookups.StatusText(item.Status);
            item.PriorityText = FreshserviceLookups.PriorityText(item.Priority);
            if (string.IsNullOrWhiteSpace(item.RequesterName)) item.RequesterName = null;
            if (string.IsNullOrWhiteSpace(item.ResponderName)) item.ResponderName = null;
        }

        return new PagedResult<TicketListItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = (int)Math.Min(total, int.MaxValue),
        };
    }

    public async Task<TicketDetail?> GetByIdAsync(long ticketId, IReadOnlyList<long> allowedGroupIds, CancellationToken ct)
    {
        // The group filter is part of the WHERE, so a ticket in a group the user
        // may not see returns null (404) — no leaking across the RH boundary.
        const string ticketSql = @"
            SELECT t.TicketID, t.Subject, t.DescriptionHTML, t.DescriptionText,
                   t.Status, t.Priority, t.Source, t.Category, t.SubCategory, t.ItemCategory,
                   t.DepartmentID, t.WorkspaceID, t.CreatedAt, t.UpdatedAt, t.DueBy, t.FrDueBy,
                   t.CustomFieldsJson,
                   t.RequesterID, t.ResponderID, t.GroupID
            FROM dbo.FS_Tickets_Archive t
            WHERE t.TicketID = @TicketId AND t.GroupID IN @AllowedGroups;";

        using var conn = _factory.Create();
        var detail = await conn.QuerySingleOrDefaultAsync<TicketDetail>(
            new CommandDefinition(ticketSql, new { TicketId = ticketId, AllowedGroups = allowedGroupIds }, cancellationToken: ct));
        if (detail is null) return null;

        // FK ids came back on the ticket row (JsonIgnored on the model); use them to hydrate relations.
        var requesterId = detail.RequesterID;
        var responderId = detail.ResponderID;
        var groupId = detail.GroupID;

        detail.StatusText = FreshserviceLookups.StatusText(detail.Status);
        detail.PriorityText = FreshserviceLookups.PriorityText(detail.Priority);
        detail.SourceText = FreshserviceLookups.SourceText(detail.Source);

        if (requesterId is > 0)
        {
            detail.Requester = await conn.QuerySingleOrDefaultAsync<PersonInfo>(new CommandDefinition(
                @"SELECT RequesterID AS ID, FirstName, LastName, PrimaryEmail AS Email, JobTitle, Active
                  FROM dbo.FS_Requesters_Archive WHERE RequesterID = @Id",
                new { Id = requesterId }, cancellationToken: ct));
        }
        if (responderId is > 0)
        {
            detail.Responder = await conn.QuerySingleOrDefaultAsync<PersonInfo>(new CommandDefinition(
                @"SELECT ResponderID AS ID, FirstName, LastName, Email, JobTitle, Active
                  FROM dbo.FS_Responders_Archive WHERE ResponderID = @Id",
                new { Id = responderId }, cancellationToken: ct));
        }
        if (groupId is > 0)
        {
            detail.Group = await conn.QuerySingleOrDefaultAsync<GroupInfo>(new CommandDefinition(
                @"SELECT GroupID, Name, Description FROM dbo.FS_Groups_Archive WHERE GroupID = @Id",
                new { Id = groupId }, cancellationToken: ct));
        }

        detail.Conversations = (await conn.QueryAsync<ConversationItem>(new CommandDefinition(
            @"SELECT ConversationID, TicketID, UserID, FromEmail, ToEmailsJson, CcEmailsJson,
                     BodyHTML, BodyText, PrivateNote, Incoming, ConversationType, CreatedAt, UpdatedAt
              FROM dbo.FS_Ticket_Conversations_Archive
              WHERE TicketID = @TicketId
              ORDER BY CreatedAt ASC, ConversationID ASC",
            new { TicketId = ticketId }, cancellationToken: ct))).AsList();

        var attachments = (await conn.QueryAsync<AttachmentItem>(new CommandDefinition(
            @"SELECT AttachmentArchiveID AS AttachmentID, FreshserviceAttachmentID, TicketID, ConversationID,
                     FileName, ContentType, FileSize, Downloaded, LocalFilePath
              FROM dbo.FS_Ticket_Attachments_Archive
              WHERE TicketID = @TicketId
              ORDER BY ConversationID, AttachmentArchiveID",
            new { TicketId = ticketId }, cancellationToken: ct))).AsList();

        // Only offer a download if the file actually resolves on disk (via configured root).
        foreach (var a in attachments)
        {
            a.CanDownload = ResolveAttachmentPath(a.LocalFilePath) is not null;
        }
        detail.Attachments = attachments;

        return detail;
    }

    public async Task<IReadOnlyList<GroupLookup>> GetGroupsAsync(IReadOnlyList<long> allowedGroupIds, CancellationToken ct)
    {
        using var conn = _factory.Create();
        var groups = await conn.QueryAsync<GroupLookup>(new CommandDefinition(
            @"SELECT g.GroupID, g.Name
              FROM dbo.FS_Groups_Archive g
              WHERE g.GroupID IN @AllowedGroups
              ORDER BY g.Name",
            new { AllowedGroups = allowedGroupIds }, cancellationToken: ct));
        return groups.AsList();
    }

    public async Task<AttachmentFile?> GetAttachmentFileAsync(long attachmentId, IReadOnlyList<long> allowedGroupIds, CancellationToken ct)
    {
        using var conn = _factory.Create();
        // Verify the attachment's ticket is in a group the user may see before returning any path.
        var row = await conn.QuerySingleOrDefaultAsync<(string? LocalFilePath, string? FileName, string? ContentType)>(
            new CommandDefinition(
                @"SELECT a.LocalFilePath, a.FileName, a.ContentType
                  FROM dbo.FS_Ticket_Attachments_Archive a
                  INNER JOIN dbo.FS_Tickets_Archive t ON t.TicketID = a.TicketID
                  WHERE a.AttachmentArchiveID = @Id AND t.GroupID IN @AllowedGroups",
                new { Id = attachmentId, AllowedGroups = allowedGroupIds }, cancellationToken: ct));

        var resolved = ResolveAttachmentPath(row.LocalFilePath);
        if (resolved is null) return null;
        return new AttachmentFile(resolved, row.FileName ?? "attachment", row.ContentType);
    }

    /// <summary>
    /// Builds a safe CONTAINS search condition from free user input. Each word becomes a
    /// prefix term ("word*") and words are AND-ed, so "reset password" matches descriptions
    /// containing both. Returns null when there is nothing searchable. The value is passed
    /// as a parameter (never string-concatenated into SQL), and quotes/noise are stripped so
    /// it can't break the full-text predicate.
    /// </summary>
    private static string? BuildContainsTerm(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;

        var words = input
            .Split(new[] { ' ', '\t', '\r', '\n', ',', ';', '"', '\'', '(', ')', '*', '&', '|' },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 0)
            .Take(10)
            .Select(w => "\"" + w + "*\"");

        var term = string.Join(" AND ", words);
        return string.IsNullOrWhiteSpace(term) ? null : term;
    }

    /// <summary>
    /// Returns a readable path for a stored attachment, or null if the file cannot be found.
    /// Tries the stored path first (already a UNC path), then rebases the portion after
    /// "Attachements" onto the configured <see cref="ArchiveOptions.AttachmentRoot"/> — so the
    /// same archive works whether the app runs on vm-trm-live (UNC) or vm-trm-sql1 (F:\).
    /// </summary>
    private string? ResolveAttachmentPath(string? storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath)) return null;

        try
        {
            if (File.Exists(storedPath)) return storedPath;

            var root = _archive.AttachmentRoot;
            if (!string.IsNullOrWhiteSpace(root))
            {
                const string marker = "Attachements";
                var idx = storedPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var relative = storedPath[(idx + marker.Length)..].TrimStart('\\', '/');
                    var candidate = Path.Combine(root, relative);
                    if (File.Exists(candidate)) return candidate;
                }
            }
        }
        catch (IOException) { /* share unreachable → treat as unavailable */ }
        catch (UnauthorizedAccessException) { /* no permission → treat as unavailable */ }

        return null;
    }
}
