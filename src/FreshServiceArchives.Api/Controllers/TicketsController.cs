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
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(ITicketRepository repo, ILogger<TicketsController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <summary>Search/list archived tickets with paging and filters.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TicketListItem>>> Search(
        [FromQuery] string? search,
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
        var query = new TicketSearchQuery
        {
            Search = search,
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
        };
        var result = await _repo.SearchAsync(query, ct);
        return Ok(result);
    }

    /// <summary>Full detail for one ticket, including conversations and attachments.</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult<TicketDetail>> GetById(long id, CancellationToken ct)
    {
        var detail = await _repo.GetByIdAsync(id, ct);
        return detail is null ? NotFound() : Ok(detail);
    }

    /// <summary>Download a locally-archived attachment file.</summary>
    [HttpGet("attachments/{attachmentId:long}")]
    public async Task<IActionResult> DownloadAttachment(long attachmentId, CancellationToken ct)
    {
        var file = await _repo.GetAttachmentFileAsync(attachmentId, ct);
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
