using System.Text.Json.Serialization;

namespace FreshServiceArchives.Api.Models;

/// <summary>A single row in the ticket search results grid.</summary>
public sealed class TicketListItem
{
    public long TicketID { get; set; }
    public string? Subject { get; set; }
    public int? Status { get; set; }
    public string? StatusText { get; set; }
    public int? Priority { get; set; }
    public string? PriorityText { get; set; }
    public long? GroupID { get; set; }
    public string? GroupName { get; set; }
    public long? RequesterID { get; set; }
    public string? RequesterName { get; set; }
    public string? RequesterEmail { get; set; }
    public long? ResponderID { get; set; }
    public string? ResponderName { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>A paged result set of tickets.</summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

/// <summary>Full ticket detail including people, conversations and attachments.</summary>
public sealed class TicketDetail
{
    public long TicketID { get; set; }
    public string? Subject { get; set; }
    public string? DescriptionHTML { get; set; }
    public string? DescriptionText { get; set; }
    public int? Status { get; set; }
    public string? StatusText { get; set; }
    public int? Priority { get; set; }
    public string? PriorityText { get; set; }
    public int? Source { get; set; }
    public string? SourceText { get; set; }
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public string? ItemCategory { get; set; }
    public long? DepartmentID { get; set; }
    public long? WorkspaceID { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DueBy { get; set; }
    public DateTime? FrDueBy { get; set; }
    public string? CustomFieldsJson { get; set; }

    // Foreign-key ids used server-side to hydrate related entities; not sent to the client.
    [JsonIgnore] public long? RequesterID { get; set; }
    [JsonIgnore] public long? ResponderID { get; set; }
    [JsonIgnore] public long? GroupID { get; set; }

    public PersonInfo? Requester { get; set; }
    public PersonInfo? Responder { get; set; }
    public GroupInfo? Group { get; set; }

    public IReadOnlyList<ConversationItem> Conversations { get; set; } = Array.Empty<ConversationItem>();
    public IReadOnlyList<AttachmentItem> Attachments { get; set; } = Array.Empty<AttachmentItem>();
}

public sealed class PersonInfo
{
    public long ID { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? JobTitle { get; set; }
    public bool? Active { get; set; }

    public string FullName =>
        string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public sealed class GroupInfo
{
    public long GroupID { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public sealed class ConversationItem
{
    public long ConversationID { get; set; }
    public long TicketID { get; set; }
    public long? UserID { get; set; }
    public string? FromEmail { get; set; }
    public string? ToEmailsJson { get; set; }
    public string? CcEmailsJson { get; set; }
    public string? BodyHTML { get; set; }
    public string? BodyText { get; set; }
    public bool PrivateNote { get; set; }
    public bool Incoming { get; set; }
    public string? ConversationType { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class AttachmentItem
{
    public long AttachmentID { get; set; }
    public long? FreshserviceAttachmentID { get; set; }
    public long TicketID { get; set; }
    public long? ConversationID { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? FileSize { get; set; }
    public bool Downloaded { get; set; }
    /// <summary>True when a local file exists on disk and can be served for download.</summary>
    public bool CanDownload { get; set; }

    // Server-side only: absolute path on the archive file share; never serialized to the client.
    [JsonIgnore] public string? LocalFilePath { get; set; }
}

public sealed class GroupLookup
{
    public long GroupID { get; set; }
    public string? Name { get; set; }
}

public sealed class CodeLookup
{
    public int Value { get; set; }
    public string Text { get; set; } = "";
}
