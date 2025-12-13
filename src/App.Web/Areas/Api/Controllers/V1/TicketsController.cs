using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Models;
using App.Application.Common.Security;
using App.Application.Tickets;
using App.Application.Tickets.Commands;
using App.Application.Tickets.Queries;
using App.Web.Authentication;

namespace App.Web.Areas.Api.Controllers.V1;

[Authorize(Policy = AppApiAuthorizationHandler.POLICY_PREFIX + RaythaClaimTypes.IsAdmin)]
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
        var query = new GetTickets.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy ?? "CreationTime desc",
            Status = status,
            Priority = priority,
            AssigneeId = !string.IsNullOrEmpty(assigneeId) ? new ShortGuid(assigneeId) : null,
            TeamId = !string.IsNullOrEmpty(teamId) ? new ShortGuid(teamId) : null,
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
        var command = new CreateTicket.Command
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority ?? "normal",
            Category = request.Category,
            Tags = request.Tags,
            OwningTeamId = !string.IsNullOrEmpty(request.OwningTeamId)
                ? new ShortGuid(request.OwningTeamId)
                : null,
            AssigneeId = !string.IsNullOrEmpty(request.AssigneeId)
                ? new ShortGuid(request.AssigneeId)
                : null,
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
        var command = new UpdateTicket.Command
        {
            Id = id,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Category = request.Category,
            Tags = request.Tags,
            OwningTeamId = !string.IsNullOrEmpty(request.OwningTeamId)
                ? new ShortGuid(request.OwningTeamId)
                : null,
            AssigneeId = !string.IsNullOrEmpty(request.AssigneeId)
                ? new ShortGuid(request.AssigneeId)
                : null,
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
        var command = new AssignTicket.Command
        {
            Id = id,
            OwningTeamId = !string.IsNullOrEmpty(request.OwningTeamId)
                ? new ShortGuid(request.OwningTeamId)
                : null,
            AssigneeId = !string.IsNullOrEmpty(request.AssigneeId)
                ? new ShortGuid(request.AssigneeId)
                : null,
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
}

// Request/Response DTOs
public record CreateTicketRequest
{
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public string? Priority { get; init; }
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

