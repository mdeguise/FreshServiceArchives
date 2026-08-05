using System.Security.Claims;
using Dapper;
using Microsoft.Extensions.Options;

namespace FreshServiceArchives.Api.Data;

/// <summary>Which access tiers a user belongs to. Full implies portal-admin rights.</summary>
public sealed record UserAccessInfo(string UserName, bool IsRh, bool IsIt, bool IsFull)
{
    public bool IsAdmin => IsFull;
    public bool HasAnyAccess => IsRh || IsIt || IsFull;
}

/// <summary>Resolves which archive groups the current Windows user may view.</summary>
public interface IUserAccessService
{
    /// <summary>The Windows sign-in name without domain, lower-cased (e.g. "mdeguise").</summary>
    string GetUserName(ClaimsPrincipal user);

    /// <summary>The tiers the user belongs to (RH / IT / Full).</summary>
    Task<UserAccessInfo> GetAccessInfoAsync(ClaimsPrincipal user, CancellationToken ct);

    /// <summary>Group ids this user may see — the union of their tiers. Every query is constrained to these.</summary>
    Task<IReadOnlyList<long>> GetAllowedGroupIdsAsync(ClaimsPrincipal user, CancellationToken ct);
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

    public async Task<UserAccessInfo> GetAccessInfoAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var userName = GetUserName(user);
        if (string.IsNullOrEmpty(userName))
            return new UserAccessInfo("", false, false, false);

        using var conn = _factory.Create();
        var row = await conn.QuerySingleAsync<(bool IsRh, bool IsIt, bool IsFull)>(new CommandDefinition(
            @"SELECT
                CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.FS_Portal_RH_Access   WHERE LOWER(UserName)=@u) THEN 1 ELSE 0 END AS bit) AS IsRh,
                CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.FS_Portal_IT_Access   WHERE LOWER(UserName)=@u) THEN 1 ELSE 0 END AS bit) AS IsIt,
                CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.FS_Portal_Full_Access WHERE LOWER(UserName)=@u) THEN 1 ELSE 0 END AS bit) AS IsFull",
            new { u = userName }, cancellationToken: ct));

        return new UserAccessInfo(userName, row.IsRh, row.IsIt, row.IsFull);
    }

    public async Task<IReadOnlyList<long>> GetAllowedGroupIdsAsync(ClaimsPrincipal user, CancellationToken ct)
    {
        var info = await GetAccessInfoAsync(user, ct);

        // Union the grants of every tier the user belongs to. No tier => empty => sees nothing.
        var allowed = new HashSet<long>();
        if (info.IsFull) foreach (var id in _access.AllowedGroupIds) allowed.Add(id);
        if (info.IsRh) foreach (var id in _access.RhGroupIds) allowed.Add(id);
        if (info.IsIt) foreach (var id in _access.ItGroupIds) allowed.Add(id);

        return allowed.ToArray();
    }
}
