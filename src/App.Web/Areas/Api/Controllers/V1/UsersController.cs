using App.Application.Common.Models;
using App.Application.Users;
using App.Application.Users.Commands;
using App.Application.Users.Queries;
using App.Domain.Entities;
using App.Web.Authentication;
using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Web.Areas.Api.Controllers.V1;

[Authorize(
    Policy = AppApiAuthorizationHandler.POLICY_PREFIX
        + BuiltInSystemPermission.MANAGE_USERS_PERMISSION
)]
public class UsersController : BaseController
{
    [HttpGet("", Name = "GetUsers")]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<UserDto>>>> GetUsers(
        [FromQuery] GetUsers.Query request
    )
    {
        var response = await Mediator.Send(request) as QueryResponseDto<ListResultDto<UserDto>>;
        return Ok(response!.Result);
    }

    [HttpGet("{userId}", Name = "GetUserById")]
    public async Task<ActionResult<IQueryResponseDto<UserDto>>> GetUserById(string userId)
    {
        var input = new GetUserById.Query { Id = userId };
        var response = await Mediator.Send(input) as QueryResponseDto<UserDto>;
        return Ok(response!.Result);
    }

    [HttpPost("", Name = "CreateUser")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> CreateUser(
        [FromBody] CreateUser.Command request
    )
    {
        var response = await Mediator.Send(request);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }
        return CreatedAtAction(
            nameof(GetUserById),
            new { userId = response.Result },
            response.Result
        );
    }

    [HttpPut("{userId}", Name = "EditUser")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditUser(
        string userId,
        [FromBody] EditUser.Command request
    )
    {
        var input = request with { Id = userId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }
        return Ok(response.Result);
    }

    [HttpDelete("{userId}", Name = "DeleteUser")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var input = new DeleteUser.Command { Id = userId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }
        return NoContent();
    }

    [HttpPut("{userId}/password", Name = "ResetPassword")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> ResetPassword(
        string userId,
        [FromBody] ResetPasswordRequestModel request
    )
    {
        var input = new ResetPassword.Command
        {
            Id = userId,
            SendEmail = request.SendEmail,
            NewPassword = request.NewPassword,
            ConfirmNewPassword = request.NewPassword,
        };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }
        return Ok(response.Result);
    }

    [HttpPut("{userId}/is-active", Name = "SetIsActive")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> SetIsActive(
        string userId,
        [FromBody] SetIsActive.Command request
    )
    {
        var input = request with { Id = userId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(new { error = response.Error });
        }
        return Ok(response.Result);
    }
}

public record ResetPasswordRequestModel
{
    public string NewPassword { get; init; }
    public bool SendEmail { get; init; } = true;
}
