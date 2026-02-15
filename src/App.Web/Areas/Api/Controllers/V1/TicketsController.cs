using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Models;
using App.Application.Common.Security;
using App.Application.Common.Utils;
using App.Application.MediaItems.Commands;
using App.Application.Tickets;
using App.Application.Tickets.Commands;
using App.Application.Tickets.Queries;
using App.Web.Authentication;

namespace App.Web.Areas.Api.Controllers.V1;

[Authorize(Policy = AppApiAuthorizationHandler.POLICY_PREFIX + AppClaimTypes.IsAdmin)]
public class TicketsController : BaseController
{
    /// <summary>
    /// Get a paginated list of tickets.
    /// </summary>
    [HttpGet(Name = "GetTickets")]
    [ProducesResponseType(typeof(ListResultDto<TicketListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListResultDto<TicketListItemDto>>> GetTickets(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? orderBy = null,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? assigneeId = null,
        [FromQuery] string? teamId = null,
        [FromQuery] long? contactId = null,
        [FromQuery] bool? unassigned = null
    )
    {
        // Parse nullable ShortGuids - only create if string has value
        ShortGuid? parsedAssigneeId = null;
        ShortGuid? parsedTeamId = null;
        
        if (!string.IsNullOrEmpty(assigneeId))
            parsedAssigneeId = new ShortGuid(assigneeId);
        
        if (!string.IsNullOrEmpty(teamId))
            parsedTeamId = new ShortGuid(teamId);
        
        var query = new GetTickets.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy ?? "CreationTime desc",
            Status = status,
            Priority = priority,
            AssigneeId = parsedAssigneeId,
            TeamId = parsedTeamId,
            ContactId = contactId,
            Unassigned = unassigned,
        };

        var response = await Mediator.Send(query);
        return Ok(response.Result);
    }

    /// <summary>
    /// Get a single ticket by ID.
    /// </summary>
    [HttpGet("{id:long}", Name = "GetTicketById")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> GetTicketById(long id)
    {
        var query = new GetTicketById.Query { Id = id };
        var response = await Mediator.Send(query);
        return Ok(response.Result);
    }

    /// <summary>
    /// Create a new ticket.
    /// </summary>
    [HttpPost(Name = "CreateTicket")]
    [ProducesResponseType(typeof(CreateTicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateTicketResponse>> CreateTicket(
        [FromBody] CreateTicketRequest request
    )
    {
        // Parse nullable ShortGuids - only create if string has value
        ShortGuid? owningTeamId = null;
        ShortGuid? assigneeId = null;
        
        if (!string.IsNullOrEmpty(request.OwningTeamId))
            owningTeamId = new ShortGuid(request.OwningTeamId);
        
        if (!string.IsNullOrEmpty(request.AssigneeId))
            assigneeId = new ShortGuid(request.AssigneeId);
        
        var command = new CreateTicket.Command
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority ?? "normal",
            Language = request.Language ?? "english",
            Category = request.Category,
            Tags = request.Tags,
            OwningTeamId = owningTeamId,
            AssigneeId = assigneeId,
            ContactId = request.ContactId,
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return CreatedAtAction(
            nameof(GetTicketById),
            new { id = response.Result },
            new CreateTicketResponse { Id = response.Result }
        );
    }

    /// <summary>
    /// Update an existing ticket.
    /// </summary>
    [HttpPut("{id:long}", Name = "UpdateTicket")]
    [ProducesResponseType(typeof(UpdateTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateTicketResponse>> UpdateTicket(
        long id,
        [FromBody] UpdateTicketRequest request
    )
    {
        // Parse nullable ShortGuids - only create if string has value
        ShortGuid? owningTeamId = null;
        ShortGuid? assigneeId = null;
        
        if (!string.IsNullOrEmpty(request.OwningTeamId))
            owningTeamId = new ShortGuid(request.OwningTeamId);
        
        if (!string.IsNullOrEmpty(request.AssigneeId))
            assigneeId = new ShortGuid(request.AssigneeId);
        
        var command = new UpdateTicket.Command
        {
            Id = id,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Language = request.Language,
            Category = request.Category,
            Tags = request.Tags,
            OwningTeamId = owningTeamId,
            AssigneeId = assigneeId,
            ContactId = request.ContactId,
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(new UpdateTicketResponse { Id = response.Result });
    }

    /// <summary>
    /// Delete a ticket.
    /// </summary>
    [HttpDelete("{id:long}", Name = "DeleteTicket")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTicket(long id)
    {
        var command = new DeleteTicket.Command { Id = id };
        var response = await Mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Assign a ticket to a team and/or user.
    /// </summary>
    [HttpPost("{id:long}/assign", Name = "AssignTicket")]
    [ProducesResponseType(typeof(UpdateTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateTicketResponse>> AssignTicket(
        long id,
        [FromBody] AssignTicketRequest request
    )
    {
        // Parse nullable ShortGuids - only create if string has value
        ShortGuid? owningTeamId = null;
        ShortGuid? assigneeId = null;
        
        if (!string.IsNullOrEmpty(request.OwningTeamId))
            owningTeamId = new ShortGuid(request.OwningTeamId);
        
        if (!string.IsNullOrEmpty(request.AssigneeId))
            assigneeId = new ShortGuid(request.AssigneeId);
        
        var command = new AssignTicket.Command
        {
            Id = id,
            OwningTeamId = owningTeamId,
            AssigneeId = assigneeId,
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(new UpdateTicketResponse { Id = response.Result });
    }

    /// <summary>
    /// Change the status of a ticket.
    /// </summary>
    [HttpPost("{id:long}/status", Name = "ChangeTicketStatus")]
    [ProducesResponseType(typeof(UpdateTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateTicketResponse>> ChangeTicketStatus(
        long id,
        [FromBody] ChangeStatusRequest request
    )
    {
        var command = new ChangeTicketStatus.Command { Id = id, NewStatus = request.Status };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(new UpdateTicketResponse { Id = response.Result });
    }

    /// <summary>
    /// Change the priority of a ticket.
    /// </summary>
    [HttpPost("{id:long}/priority", Name = "ChangeTicketPriority")]
    [ProducesResponseType(typeof(UpdateTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateTicketResponse>> ChangeTicketPriority(
        long id,
        [FromBody] ChangePriorityRequest request
    )
    {
        var command = new ChangeTicketPriority.Command { Id = id, NewPriority = request.Priority };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(new UpdateTicketResponse { Id = response.Result });
    }

    /// <summary>
    /// Refresh/restart the SLA for a ticket.
    /// </summary>
    [HttpPost("{id:long}/refresh-sla", Name = "RefreshTicketSla")]
    [ProducesResponseType(typeof(UpdateTicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateTicketResponse>> RefreshTicketSla(
        long id,
        [FromBody] RefreshSlaRequest request
    )
    {
        var command = new RefreshTicketSla.Command
        {
            Id = id,
            RestartFromNow = request.RestartFromNow,
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(new UpdateTicketResponse { Id = response.Result });
    }

    /// <summary>
    /// Upload a file and attach it to a ticket using multipart/form-data.
    /// </summary>
    [HttpPost("{id:long}/attachments/upload", Name = "UploadTicketAttachment")]
    [DisableRequestSizeLimit]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AddTicketAttachmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadTicketAttachment(
        long id,
        [FromForm] TicketAttachmentUploadRequest request
    )
    {
        if (request.File == null || request.File.Length <= 0)
        {
            return BadRequest(new { error = "File is required." });
        }

        var contentType = string.IsNullOrWhiteSpace(request.File.ContentType)
            ? FileStorageUtility.GetMimeType(request.File.FileName)
            : request.File.ContentType;

        using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream);
        var data = stream.ToArray();

        return await SaveAndAttach(
            id,
            request.File.FileName,
            contentType,
            data,
            request.DisplayName,
            request.Description
        );
    }

    /// <summary>
    /// Upload a base64-encoded file and attach it to a ticket.
    /// </summary>
    [HttpPost("{id:long}/attachments/base64", Name = "UploadTicketAttachmentBase64")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AddTicketAttachmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadTicketAttachmentBase64(
        long id,
        [FromBody] TicketAttachmentBase64Request request
    )
    {
        if (string.IsNullOrWhiteSpace(request.Base64Data))
        {
            return BadRequest(new { error = "Base64 data is required." });
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return BadRequest(new { error = "File name is required." });
        }

        if (!TryParseBase64Payload(request.Base64Data, out var dataUriContentType, out var data))
        {
            return BadRequest(new { error = "Invalid base64 payload." });
        }

        var contentType = string.IsNullOrWhiteSpace(request.ContentType)
            ? dataUriContentType
            : request.ContentType;

        if (string.IsNullOrWhiteSpace(contentType))
        {
            contentType = FileStorageUtility.GetMimeType(request.FileName);
        }

        return await SaveAndAttach(
            id,
            request.FileName,
            contentType,
            data,
            request.DisplayName,
            request.Description
        );
    }

    private async Task<IActionResult> SaveAndAttach(
        long ticketId,
        string fileName,
        string contentType,
        byte[] data,
        string? displayName,
        string? description
    )
    {
        if (data.Length <= 0)
        {
            return BadRequest(new { error = "File is empty." });
        }

        if (
            !FileStorageUtility.IsAllowedMimeType(
                contentType,
                FileStorageProviderSettings.AllowedMimeTypes
            )
        )
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { success = false, error = "File type is not allowed." }
            );
        }

        var idForKey = ShortGuid.NewGuid();
        var objectKey = FileStorageUtility.CreateObjectKeyFromIdAndFileName(
            idForKey.ToString(),
            fileName
        );

        await FileStorageProvider.SaveAndGetDownloadUrlAsync(
            data,
            objectKey,
            fileName,
            contentType,
            FileStorageUtility.GetDefaultExpiry()
        );

        var createMediaItem = new CreateMediaItem.Command
        {
            Id = idForKey,
            FileName = fileName,
            Length = data.Length,
            ContentType = contentType,
            FileStorageProvider = FileStorageProvider.GetName(),
            ObjectKey = objectKey,
        };

        var mediaResponse = await Mediator.Send(createMediaItem);
        if (!mediaResponse.Success)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new { success = false, error = mediaResponse.Error }
            );
        }

        var attachCommand = new AddTicketAttachment.Command
        {
            TicketId = ticketId,
            MediaItemId = idForKey,
            DisplayName = displayName,
            Description = description,
        };

        var attachResponse = await Mediator.Send(attachCommand);
        if (!attachResponse.Success)
        {
            return BadRequest(new { error = attachResponse.Error });
        }

        return Ok(
            new AddTicketAttachmentResponse
            {
                AttachmentId = attachResponse.Result.ToString(),
                MediaItemId = idForKey.ToString(),
                FileName = fileName,
                ContentType = contentType,
                SizeBytes = data.Length,
                ObjectKey = objectKey,
            }
        );
    }

    private static bool TryParseBase64Payload(
        string input,
        out string? contentType,
        out byte[] data
    )
    {
        contentType = null;
        data = Array.Empty<byte>();

        var trimmed = input.Trim();
        var rawBase64 = trimmed;

        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = trimmed.IndexOf(',');
            if (commaIndex > 0)
            {
                var header = trimmed[..commaIndex];
                rawBase64 = trimmed[(commaIndex + 1)..];

                var semicolonIndex = header.IndexOf(';');
                if (semicolonIndex > 5)
                {
                    contentType = header[5..semicolonIndex];
                }
            }
        }

        try
        {
            data = Convert.FromBase64String(rawBase64);
            return data.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

// Request/Response DTOs
public record CreateTicketRequest
{
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string? Priority { get; init; }
    public string? Language { get; init; }
    public string? Category { get; init; }
    public List<string>? Tags { get; init; }
    public string? OwningTeamId { get; init; }
    public string? AssigneeId { get; init; }
    public long? ContactId { get; init; }
}

public record UpdateTicketRequest
{
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string Priority { get; init; } = null!;
    public string Language { get; init; } = "english";
    public string? Category { get; init; }
    public List<string>? Tags { get; init; }
    public string? OwningTeamId { get; init; }
    public string? AssigneeId { get; init; }
    public long? ContactId { get; init; }
}

public record AssignTicketRequest
{
    public string? OwningTeamId { get; init; }
    public string? AssigneeId { get; init; }
}

public record ChangeStatusRequest
{
    public string Status { get; init; } = null!;
}

public record ChangePriorityRequest
{
    public string Priority { get; init; } = null!;
}

public record RefreshSlaRequest
{
    /// <summary>
    /// If true (default), recalculates SLA due date from current time (restarts the clock).
    /// If false, re-evaluates SLA rules but keeps calculation from original creation time.
    /// </summary>
    public bool RestartFromNow { get; init; } = true;
}

public record CreateTicketResponse
{
    public long Id { get; init; }
}

public record UpdateTicketResponse
{
    public long Id { get; init; }
}

public record TicketAttachmentUploadRequest
{
    public IFormFile? File { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
}

public record TicketAttachmentBase64Request
{
    public string Base64Data { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public string? ContentType { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
}public record AddTicketAttachmentResponse
{
    public string AttachmentId { get; init; } = null!;
    public string MediaItemId { get; init; } = null!;
    public string FileName { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public int SizeBytes { get; init; }
    public string ObjectKey { get; init; } = null!;
}
