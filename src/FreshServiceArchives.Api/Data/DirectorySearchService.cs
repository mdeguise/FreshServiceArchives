using System.DirectoryServices;
using System.Runtime.Versioning;

namespace FreshServiceArchives.Api.Data;

/// <summary>A person found in Active Directory.</summary>
public sealed class DirectoryUser
{
    public string UserName { get; set; } = "";      // sAMAccountName
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
}

public interface IDirectorySearchService
{
    /// <summary>Ambiguous-name-resolution search of AD users. Returns up to <paramref name="max"/> matches.</summary>
    Task<IReadOnlyList<DirectoryUser>> SearchAsync(string query, int max, CancellationToken ct);
}

public sealed class DirectorySearchService : IDirectorySearchService
{
    public Task<IReadOnlyList<DirectoryUser>> SearchAsync(string query, int max, CancellationToken ct)
    {
        query = (query ?? "").Trim();
        if (query.Length < 2 || !OperatingSystem.IsWindows())
            return Task.FromResult<IReadOnlyList<DirectoryUser>>(Array.Empty<DirectoryUser>());

        var q = query;
        var max2 = Math.Clamp(max, 1, 25);

        // DirectorySearcher is blocking; keep it off the request thread.
        return Task.Run<IReadOnlyList<DirectoryUser>>(() =>
        {
            if (!OperatingSystem.IsWindows()) return Array.Empty<DirectoryUser>();
            return Search(q, max2);
        }, ct);
    }

    [SupportedOSPlatform("windows")]
    private static List<DirectoryUser> Search(string query, int max)
    {
        // ANR (Ambiguous Name Resolution) matches against name, sign-in, email, etc.
        var filter = $"(&(objectCategory=person)(objectClass=user)(anr={EscapeLdap(query)}))";

        // Empty path => the current domain, bound as the process (app-pool) identity.
        using var root = new DirectoryEntry();
        // Note: do NOT set PageSize — that enables paged search which ignores SizeLimit
        // and returns the whole result set. SizeLimit alone caps the server response.
        using var searcher = new DirectorySearcher(root)
        {
            Filter = filter,
            SizeLimit = max,
            ClientTimeout = TimeSpan.FromSeconds(10),
            ServerTimeLimit = TimeSpan.FromSeconds(10),
        };
        searcher.PropertiesToLoad.AddRange(new[] { "sAMAccountName", "mail", "displayName" });

        var results = new List<DirectoryUser>();
        using var found = searcher.FindAll();
        foreach (SearchResult r in found)
        {
            var sam = Prop(r, "sAMAccountName");
            if (string.IsNullOrWhiteSpace(sam)) continue;   // skip non-login objects
            results.Add(new DirectoryUser
            {
                UserName = sam,
                Email = Prop(r, "mail"),
                DisplayName = Prop(r, "displayName"),
            });
            if (results.Count >= max) break;                // hard cap, regardless of server behaviour
        }

        return results
            .OrderBy(u => u.DisplayName ?? u.UserName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    [SupportedOSPlatform("windows")]
    private static string? Prop(SearchResult r, string name) =>
        r.Properties[name].Count > 0 ? r.Properties[name][0]?.ToString() : null;

    /// <summary>Escapes LDAP filter special characters (RFC 4515) to prevent filter injection.</summary>
    private static string EscapeLdap(string value)
    {
        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\': sb.Append("\\5c"); break;
                case '*': sb.Append("\\2a"); break;
                case '(': sb.Append("\\28"); break;
                case ')': sb.Append("\\29"); break;
                case '\0': sb.Append("\\00"); break;
                case '/': sb.Append("\\2f"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
