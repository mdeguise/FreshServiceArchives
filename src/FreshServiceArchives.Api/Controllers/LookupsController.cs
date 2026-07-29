using FreshServiceArchives.Api.Data;
using FreshServiceArchives.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace FreshServiceArchives.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LookupsController : ControllerBase
{
    private readonly ITicketRepository _repo;
    private readonly IUserAccessService _access;

    public LookupsController(ITicketRepository repo, IUserAccessService access)
    {
        _repo = repo;
        _access = access;
    }

    [HttpGet("groups")]
    public async Task<ActionResult<IReadOnlyList<GroupLookup>>> Groups(CancellationToken ct)
    {
        var allowed = await _access.GetAllowedGroupIdsAsync(User, ct);
        return Ok(await _repo.GetGroupsAsync(allowed, ct));
    }

    [HttpGet("statuses")]
    public ActionResult<IEnumerable<CodeLookup>> Statuses() =>
        Ok(FreshserviceLookups.AsLookups(FreshserviceLookups.Statuses));

    [HttpGet("priorities")]
    public ActionResult<IEnumerable<CodeLookup>> Priorities() =>
        Ok(FreshserviceLookups.AsLookups(FreshserviceLookups.Priorities));
}
