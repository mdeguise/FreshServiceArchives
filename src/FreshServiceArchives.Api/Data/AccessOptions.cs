namespace FreshServiceArchives.Api.Data;

/// <summary>Bound from the "Access" section of appsettings.json.</summary>
public sealed class AccessOptions
{
    public const string SectionName = "Access";

    /// <summary>Every group the portal knows about — granted to Full Access users.</summary>
    public long[] AllowedGroupIds { get; set; } = Array.Empty<long>();

    /// <summary>Groups granted to users in dbo.FS_Portal_RH_Access.</summary>
    public long[] RhGroupIds { get; set; } = Array.Empty<long>();

    /// <summary>Groups granted to users in dbo.FS_Portal_IT_Access.</summary>
    public long[] ItGroupIds { get; set; } = Array.Empty<long>();
}

/// <summary>Bound from the "Archive" section of appsettings.json.</summary>
public sealed class ArchiveOptions
{
    public const string SectionName = "Archive";

    /// <summary>Root of the attachment file store (UNC or local path).</summary>
    public string? AttachmentRoot { get; set; }
}
