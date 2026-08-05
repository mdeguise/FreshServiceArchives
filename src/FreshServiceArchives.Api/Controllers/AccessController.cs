using Dapper;
using FreshServiceArchives.Api.Data;
using Microsoft.AspNetCore.Mvc;

namespace FreshServiceArchives.Api.Controllers;

public sealed class AccessUser
{
    public string UserName { get; set; } = "";
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public DateTime? AddedAtUtc { get; set; }
    public string? AddedBy { get; set; }
}

public sealed class AddAccessUserRequest
{
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
}

[ApiController]
[Route("api/access")]
public sealed class AccessController : ControllerBase
{
    private readonly ISqlConnectionFactory _factory;
    private readonly IUserAccessService _access;
    private readonly IDirectorySearchService _directory;

    // Whitelist: tier key -> access table. Prevents any table name coming from the URL.
    private static readonly Dictionary<string, string> Tables = new(StringComparer.OrdinalIgnoreCase)
    {
        ["rh"] = "dbo.FS_Portal_RH_Access",
        ["it"] = "dbo.FS_Portal_IT_Access",
        ["full"] = "dbo.FS_Portal_Full_Access",
    };

    public AccessController(ISqlConnectionFactory factory, IUserAccessService access, IDirectorySearchService directory)
    {
        _factory = factory;
        _access = access;
        _directory = directory;
    }

    /// <summary>Search Active Directory for users to add. Admin only.</summary>
    [HttpGet("directory/search")]
    public async Task<ActionResult<IReadOnlyList<DirectoryUser>>> DirectorySearch([FromQuery] string q, CancellationToken ct)
    {
        if (!await IsAdminAsync(ct)) return Forbid();
        return Ok(await _directory.SearchAsync(q ?? "", 10, ct));
    }

    /// <summary>Current user's access tiers (used by the UI to show/hide the admin section).</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserAccessInfo>> Me(CancellationToken ct) =>
        Ok(await _access.GetAccessInfoAsync(User, ct));

    /// <summary>All three access lists. Admin (Full Access) only.</summary>
    [HttpGet("lists")]
    public async Task<ActionResult<object>> Lists(CancellationToken ct)
    {
        if (!await IsAdminAsync(ct)) return Forbid();

        using var conn = _factory.Create();
        var result = new Dictionary<string, IReadOnlyList<AccessUser>>();
        foreach (var (key, table) in Tables)
        {
            var users = await conn.QueryAsync<AccessUser>(new CommandDefinition(
                $"SELECT UserName, Email, DisplayName, AddedAtUtc, AddedBy FROM {table} ORDER BY UserName",
                cancellationToken: ct));
            result[key] = users.AsList();
        }
        return Ok(result);
    }

    /// <summary>Add a user to a tier. Admin only.</summary>
    [HttpPost("{tier}")]
    public async Task<IActionResult> Add(string tier, [FromBody] AddAccessUserRequest body, CancellationToken ct)
    {
        if (!Tables.TryGetValue(tier, out var table)) return NotFound("Unknown access tier.");
        if (!await IsAdminAsync(ct)) return Forbid();

        var userName = body.UserName?.Trim();
        if (string.IsNullOrWhiteSpace(userName))
            return BadRequest("UserName is required (the Windows sign-in name, e.g. 'jdoe').");

        using var conn = _factory.Create();
        await conn.ExecuteAsync(new CommandDefinition(
            $@"IF NOT EXISTS (SELECT 1 FROM {table} WHERE LOWER(UserName) = LOWER(@UserName))
                   INSERT INTO {table} (UserName, Email, DisplayName, AddedBy)
                   VALUES (@UserName, @Email, @DisplayName, @AddedBy)",
            new
            {
                UserName = userName,
                Email = string.IsNullOrWhiteSpace(body.Email) ? null : body.Email.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(body.DisplayName) ? null : body.DisplayName.Trim(),
                AddedBy = _access.GetUserName(User),
            }, cancellationToken: ct));

        return NoContent();
    }

    /// <summary>Remove a user from a tier. Admin only.</summary>
    [HttpDelete("{tier}/{userName}")]
    public async Task<IActionResult> Remove(string tier, string userName, CancellationToken ct)
    {
        if (!Tables.TryGetValue(tier, out var table)) return NotFound("Unknown access tier.");
        if (!await IsAdminAsync(ct)) return Forbid();

        using var conn = _factory.Create();
        await conn.ExecuteAsync(new CommandDefinition(
            $"DELETE FROM {table} WHERE LOWER(UserName) = LOWER(@UserName)",
            new { UserName = userName }, cancellationToken: ct));

        return NoContent();
    }

    private async Task<bool> IsAdminAsync(CancellationToken ct) =>
        (await _access.GetAccessInfoAsync(User, ct)).IsAdmin;
}
