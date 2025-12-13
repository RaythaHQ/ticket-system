using App.Application.Common.Models;
using App.Application.Common.Security;
using App.Application.Contacts;
using App.Application.Contacts.Commands;
using App.Application.Contacts.Queries;
using App.Web.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Api.Controllers.V1;

[Authorize(Policy = AppApiAuthorizationHandler.POLICY_PREFIX + RaythaClaimTypes.IsAdmin)]
public class ContactsController : BaseController
{
    /// <summary>
    /// Get a paginated list of contacts.
    /// </summary>
    [HttpGet(Name = "GetContacts")]
    [ProducesResponseType(typeof(ListResultDto<ContactListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListResultDto<ContactListItemDto>>> GetContacts(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? orderBy = null
    )
    {
        var query = new GetContacts.Query
        {
            Search = search,
            PageNumber = pageNumber,
            PageSize = pageSize,
            OrderBy = orderBy ?? "CreationTime desc",
        };

        var response = await Mediator.Send(query);
        return Ok(response.Result);
    }

    /// <summary>
    /// Get a single contact by ID.
    /// </summary>
    [HttpGet("{id:long}", Name = "GetContactById")]
    [ProducesResponseType(typeof(ContactDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContactDto>> GetContactById(long id)
    {
        var query = new GetContactById.Query { Id = id };
        var response = await Mediator.Send(query);
        return Ok(response.Result);
    }

    /// <summary>
    /// Create a new contact.
    /// </summary>
    [HttpPost(Name = "CreateContact")]
    [ProducesResponseType(typeof(CreateContactResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateContactResponse>> CreateContact(
        [FromBody] CreateContactRequest request
    )
    {
        var command = new CreateContact.Command
        {
            Id = request.Id,
            Name = request.Name,
            Email = request.Email,
            PhoneNumbers = request.PhoneNumbers,
            Address = request.Address,
            OrganizationAccount = request.OrganizationAccount,
            DmeIdentifiers = request.DmeIdentifiers,
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return CreatedAtAction(
            nameof(GetContactById),
            new { id = response.Result },
            new CreateContactResponse { Id = response.Result }
        );
    }

    /// <summary>
    /// Update an existing contact.
    /// </summary>
    [HttpPut("{id:long}", Name = "UpdateContact")]
    [ProducesResponseType(typeof(UpdateContactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateContactResponse>> UpdateContact(
        long id,
        [FromBody] UpdateContactRequest request
    )
    {
        var command = new UpdateContact.Command
        {
            Id = id,
            Name = request.Name,
            Email = request.Email,
            PhoneNumbers = request.PhoneNumbers,
            Address = request.Address,
            OrganizationAccount = request.OrganizationAccount,
            DmeIdentifiers = request.DmeIdentifiers,
        };

        var response = await Mediator.Send(command);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return Ok(new UpdateContactResponse { Id = response.Result });
    }

    /// <summary>
    /// Delete a contact.
    /// </summary>
    [HttpDelete("{id:long}", Name = "DeleteContact")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContact(long id)
    {
        var command = new DeleteContact.Command { Id = id };
        var response = await Mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }

        return NoContent();
    }
}

// Request/Response DTOs
public record CreateContactRequest
{
    public long? Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Email { get; init; }
    public List<string>? PhoneNumbers { get; init; }
    public string? Address { get; init; }
    public string? OrganizationAccount { get; init; }
    public Dictionary<string, string>? DmeIdentifiers { get; init; }
}

public record UpdateContactRequest
{
    public string Name { get; init; } = null!;
    public string? Email { get; init; }
    public List<string>? PhoneNumbers { get; init; }
    public string? Address { get; init; }
    public string? OrganizationAccount { get; init; }
    public Dictionary<string, string>? DmeIdentifiers { get; init; }
}

public record CreateContactResponse
{
    public long Id { get; init; }
}

public record UpdateContactResponse
{
    public long Id { get; init; }
}
