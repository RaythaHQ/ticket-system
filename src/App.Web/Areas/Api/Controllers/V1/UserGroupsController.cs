using System.Threading.Tasks;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.Common.Models;
using App.Application.UserGroups;
using App.Application.UserGroups.Commands;
using App.Application.UserGroups.Queries;
using App.Domain.Entities;
using App.Web.Authentication;

namespace App.Web.Areas.Api.Controllers.V1;

[Authorize(
    Policy = AppApiAuthorizationHandler.POLICY_PREFIX
        + BuiltInSystemPermission.MANAGE_USERS_PERMISSION
)]
public class UserGroupsController : BaseController
{
    [HttpGet("", Name = "GetUserGroups")]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<UserGroupDto>>>> GetUserGroups(
        [FromQuery] GetUserGroups.Query request
    )
    {
        var response =
            await Mediator.Send(request) as QueryResponseDto<ListResultDto<UserGroupDto>>;
        return response;
    }

    [HttpGet("{userGroupId}", Name = "GetUserGroupById")]
    public async Task<ActionResult<IQueryResponseDto<UserGroupDto>>> GetUserGroupById(
        string userGroupId
    )
    {
        var input = new GetUserGroupById.Query { Id = userGroupId };
        var response = await Mediator.Send(input) as QueryResponseDto<UserGroupDto>;
        return response;
    }

    [HttpPost("", Name = "CreateUserGroup")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateUserGroup(
        [FromBody] CreateUserGroup.Command request
    )
    {
        var response = await Mediator.Send(request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(
            nameof(GetUserGroupById),
            new { userGroupId = response.Result },
            response
        );
    }

    [HttpPut("{userGroupId}", Name = "EditUserGroup")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditUserGroup(
        string userGroupId,
        [FromBody] EditUserGroup.Command request
    )
    {
        var input = request with { Id = userGroupId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpDelete("{userGroupId}", Name = "DeleteUserGroup")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> DeleteUserGroup(
        string userGroupId
    )
    {
        var input = new DeleteUserGroup.Command { Id = userGroupId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }
}
