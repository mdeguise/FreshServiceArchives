using FreshServiceArchives.Api.Data;
using FreshServiceArchives.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace FreshServiceArchives.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketRepository _repo;
    private readonly IUserAccessService _access;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(ITicketRepository repo, IUserAccessService access, ILogger<TicketsController> logger)
    {
        _repo = repo;
        _access = access;
        _logger = logger;
    }

    /// <summary>Search/list archived tickets with paging and filters.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketListItem>>> Search(
        [FromQuery] string? search,
        [FromQuery] string? requester,
        [FromQuery] string? description,
        [FromQuery] int? status,
        [FromQuery] int? priority,
        [FromQuery] long? groupId,
        [FromQuery] DateTime? createdFrom,
        [FromQuery] DateTime? createdTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string sortBy = "UpdatedAt",
        [FromQuery] bool sortDesc = true,
        CancellationToken ct = default)
    {
        var allowed = await _access.GetAllowedGroupIdsAsync(User, ct);

        // If the caller asked for a specific group they aren't allowed to see, deny it.
        if (groupId is not null && !allowed.Contains(groupId.Value))
        {
            return Ok(new PagedResult<TicketListItem> { Page = page, PageSize = pageSize });
        }

        var query = new TicketSearchQuery
        {
            Search = search,
            Requester = requester,
            Description = description,
            Status = status,
            Priority = priority,
            GroupId = groupId,
            CreatedFrom = createdFrom,
            // Treat createdTo as inclusive of the whole day by pushing to the next midnight.
            CreatedTo = createdTo?.Date.AddDays(1),
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDesc = sortDesc,
            AllowedGroupIds = allowed,
        };
        var result = await _repo.SearchAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Full detail for one ticket, including conversations and attachments.</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<TicketDetail>> GetById(long id, CancellationToken ct)
    {
        var allowed = await _access.GetAllowedGroupIdsAsync(User, ct);
        var detail = await _repo.GetByIdAsync(id, allowed, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    /// <summary>Download a locally-archived attachment file.</summary>
    [HttpGet("attachments/{attachmentId:long}")]
    public async Task<IActionResult> DownloadAttachment(long attachmentId, CancellationToken ct)
    {
        var allowed = await _access.GetAllowedGroupIdsAsync(User, ct);
        var file = await _repo.GetAttachmentFileAsync(attachmentId, allowed, ct);
        if (file is null || !System.IO.File.Exists(file.LocalFilePath))
        {
            return NotFound("Attachment file is not available in the archive.");
        }

        var contentType = file.ContentType;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            if (!new FileExtensionContentTypeProvider().TryGetContentType(file.FileName, out contentType))
                contentType = "application/octet-stream";
        }

        var stream = new FileStream(file.LocalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, contentType!, file.FileName);
    }
}
