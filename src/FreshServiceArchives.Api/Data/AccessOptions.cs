namespace FreshServiceArchives.Api.Data;

/// <summary>Bound from the "Access" section of appsettings.json.</summary>
public sealed class AccessOptions
{
    public const string SectionName = "Access";

    /// <summary>The only groups selectable/visible anywhere in the portal.</summary>
    public long[] AllowedGroupIds { get; set; } = Array.Empty<long>();

    /// <summary>RH groups visible only to users listed in dbo.FS_Portal_RH_Access.</summary>
    public long[] RestrictedGroupIds { get; set; } = Array.Empty<long>();
}

/// <summary>Bound from the "Archive" section of appsettings.json.</summary>
public sealed class ArchiveOptions
{
    public const string SectionName = "Archive";

    /// <summary>Root of the attachment file store (UNC or local path).</summary>
    public string? AttachmentRoot { get; set; }
}
