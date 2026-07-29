using FreshServiceArchives.Api.Data;
using FreshServiceArchives.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace FreshServiceArchives.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LookupsController : ControllerBase
{
    private readonly ITicketRepository _repo;

    public LookupsController(ITicketRepository repo) => _repo = repo;

    [HttpGet("groups")]
    public async Task<ActionResult<IReadOnlyList<GroupLookup>>> Groups(CancellationToken ct) =>
        Ok(await _repo.GetGroupsAsync(ct));

    [HttpGet("statuses")]
    public ActionResult<IEnumerable<CodeLookup>> Statuses() =>
        Ok(FreshserviceLookups.AsLookups(FreshserviceLookups.Statuses));

    [HttpGet("priorities")]
    public ActionResult<IEnumerable<CodeLookup>> Priorities() =>
        Ok(FreshserviceLookups.AsLookups(FreshserviceLookups.Priorities));
}
