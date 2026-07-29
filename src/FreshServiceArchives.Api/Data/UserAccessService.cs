using System.Security.Claims;
using Dapper;
using Microsoft.Extensions.Options;

namespace FreshServiceArchives.Api.Data;

/// <summary>Resolves which archive groups the current Windows user may view.</summary>
public interface IUserAccessService
{
    /// <summary>The Windows sign-in name without domain, lower-cased (e.g. "mdeguise").</summary>
    string GetUserName(ClaimsPrincipal user);

    /// <summary>Group ids this user is allowed to see. Every query must be constrained to these.</summary>
    Task<IReadOnlyList<long>> GetAllowedGroupIdsAsync(ClaimsPrincipal user, CancellationToken ct);

    /// <summary>True if the user is in dbo.FS_Portal_RH_Access (may see RH-restricted groups).</summary>
    Task<bool> IsRhViewerAsync(ClaimsPrincipal user, CancellationToken ct);
}

public sealed class UserAccessService : IUserAccessService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly AccessOptions _access;

    public UserAccessService(ISqlConnectionFactory factory, IOptions<AccessOptions> access)
    {
        _factory = factory;
        _access = access.Value;
    }

    public string GetUserName(ClaimsPrincipal user)
    {
        var name = user.Identity?.Name ?? "";
        var slash = name.IndexOf('\\');
        if (slash >= 0) name = name[(slash + 1)..];
        return name.Trim().ToLowerInvariant();
    }

    public async Task<bool> IsRhViewerAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var userName = GetUserName(user);
        if (string.IsNullOrEmpty(userName)) return false;

        using var conn = _factory.Create();
        var count = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM dbo.FS_Portal_RH_Access WHERE LOWER(UserName) = @UserName",
            new { UserName = userName }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<IReadOnlyList<long>> GetAllowedGroupIdsAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var restricted = new HashSet<long>(_access.RestrictedGroupIds);

        // Base set: every allowed group that is NOT RH-restricted.
        var allowed = _access.AllowedGroupIds.Where(id => !restricted.Contains(id)).ToHashSet();

        // RH viewers additionally get the restricted groups.
        if (await IsRhViewerAsync(user, ct))
        {
            foreach (var id in _access.RestrictedGroupIds) allowed.Add(id);
        }

        return allowed.ToArray();
    }
}
