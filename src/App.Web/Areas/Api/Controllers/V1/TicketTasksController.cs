using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Models;
using App.Application.Common.Security;
using App.Application.TicketTasks;
using App.Application.TicketTasks.Commands;
using App.Application.TicketTasks.Queries;
using App.Web.Authentication;

namespace App.Web.Areas.Api.Controllers.V1;

[Authorize(Policy = AppApiAuthorizationHandler.POLICY_PREFIX + RaythaClaimTypes.IsAdmin)]
[Route("app/api/v1/tickets/{ticketId:long}/tasks")]
public class TicketTasksController : BaseController
{
    /// <summary>
    /// Get all tasks for a ticket.
    /// </summary>
    [HttpGet(Name = "GetTicketTasks")]
    [ProducesResponseType(typeof(IEnumerable<TicketTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TicketTaskDto>>> GetTicketTasks(long ticketId)
    {
        var query = new GetTasksByTicketId.Query { TicketId = ticketId };
        var response = await Mediator.Send(query);
        return Ok(response.Result);
    }

    /// <summary>
    /// Create a new task on a ticket.
    /// </summary>
    [HttpPost(Name = "CreateTicketTask")]
    [ProducesResponseType(typeof(TicketTaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketTaskDto>> CreateTicketTask(
        long ticketId,
        [FromBody] CreateTicketTaskRequest request
    )
    {
        var command = new CreateTicketTask.Command
        {
            TicketId = ticketId,
            Title = request.Title,
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return CreatedAtAction(
            nameof(GetTicketTasks),
            new { ticketId },
            response.Result
        );
    }

    /// <summary>
    /// Update an existing task.
    /// </summary>
    [HttpPut("{taskId}", Name = "UpdateTicketTask")]
    [ProducesResponseType(typeof(TicketTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketTaskDto>> UpdateTicketTask(
        long ticketId,
        string taskId,
        [FromBody] UpdateTicketTaskRequest request
    )
    {
        var command = new UpdateTicketTask.Command
        {
            TaskId = new ShortGuid(taskId),
            Title = request.Title,
            AssigneeId = !string.IsNullOrEmpty(request.AssigneeId)
                ? new ShortGuid(request.AssigneeId)
                : null,
            OwningTeamId = !string.IsNullOrEmpty(request.OwningTeamId)
                ? new ShortGuid(request.OwningTeamId)
                : null,
            DueAt = request.DueAt,
            DependsOnTaskId = !string.IsNullOrEmpty(request.DependsOnTaskId)
                ? new ShortGuid(request.DependsOnTaskId)
                : null,
            ClearAssignee = request.ClearAssignee,
            ClearDueAt = request.ClearDueAt,
            ClearDependency = request.ClearDependency,
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(response.Result);
    }

    /// <summary>
    /// Mark a task as complete (closed).
    /// </summary>
    [HttpPut("{taskId}/complete", Name = "CompleteTicketTask")]
    [ProducesResponseType(typeof(TicketTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketTaskDto>> CompleteTicketTask(
        long ticketId,
        string taskId
    )
    {
        var command = new CompleteTicketTask.Command
        {
            TaskId = new ShortGuid(taskId),
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(response.Result);
    }

    /// <summary>
    /// Reopen a completed task.
    /// </summary>
    [HttpPut("{taskId}/reopen", Name = "ReopenTicketTask")]
    [ProducesResponseType(typeof(TicketTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketTaskDto>> ReopenTicketTask(
        long ticketId,
        string taskId
    )
    {
        var command = new ReopenTicketTask.Command
        {
            TaskId = new ShortGuid(taskId),
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(response.Result);
    }

    /// <summary>
    /// Delete a task (soft delete).
    /// </summary>
    [HttpDelete("{taskId}", Name = "DeleteTicketTask")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteTicketTask(
        long ticketId,
        string taskId
    )
    {
        var command = new DeleteTicketTask.Command
        {
            TaskId = new ShortGuid(taskId),
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return NoContent();
    }
}

#region Request DTOs

public record CreateTicketTaskRequest
{
    public string Title { get; init; } = null!;
}

public record UpdateTicketTaskRequest
{
    public string? Title { get; init; }
    public string? AssigneeId { get; init; }
    public string? OwningTeamId { get; init; }
    public DateTime? DueAt { get; init; }
    public string? DependsOnTaskId { get; init; }
    public bool ClearAssignee { get; init; }
    public bool ClearDueAt { get; init; }
    public bool ClearDependency { get; init; }
}

#endregion
