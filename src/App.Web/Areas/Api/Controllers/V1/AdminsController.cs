using CSharpVitamins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.Application.Admins;
using App.Application.Admins.Commands;
using App.Application.Admins.Queries;
using App.Application.Common.Models;
using App.Domain.Entities;
using App.Web.Authentication;

namespace App.Web.Areas.Api.Controllers.V1;

[Authorize(
    Policy = AppApiAuthorizationHandler.POLICY_PREFIX
        + BuiltInSystemPermission.MANAGE_ADMINISTRATORS_PERMISSION
)]
public class AdminsController : BaseController
{
    [HttpGet("", Name = "GetAdmins")]
    public async Task<ActionResult<IQueryResponseDto<ListResultDto<AdminDto>>>> GetAdmins(
        [FromQuery] GetAdmins.Query request
    )
    {
        var response = await Mediator.Send(request) as QueryResponseDto<ListResultDto<AdminDto>>;
        return response;
    }

    [HttpGet("{adminId}", Name = "GetAdminById")]
    public async Task<ActionResult<IQueryResponseDto<AdminDto>>> GetAdminById(string adminId)
    {
        var input = new GetAdminById.Query { Id = adminId };
        var response = await Mediator.Send(input) as QueryResponseDto<AdminDto>;
        return response;
    }

    [HttpPut("{adminId}", Name = "EditAdmin")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> EditAdmin(
        string adminId,
        [FromBody] EditAdmin.Command request
    )
    {
        var input = request with { Id = adminId };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }

    [HttpPut("{adminId}/custom-attributes", Name = "UpdateAdminCustomAttributes")]
    public async Task<ActionResult<ICommandResponseDto<ShortGuid>>> UpdateCustomAttributes(
        string adminId,
        [FromBody] UpdateCustomAttributesRequest request
    )
    {
        var input = new UpdateAdminCustomAttributes.Command
        {
            Id = adminId,
            CustomAttribute1 = request.CustomAttribute1,
            CustomAttribute2 = request.CustomAttribute2,
            CustomAttribute3 = request.CustomAttribute3,
            CustomAttribute4 = request.CustomAttribute4,
            CustomAttribute5 = request.CustomAttribute5,
        };
        var response = await Mediator.Send(input);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return response;
    }
}

public record UpdateCustomAttributesRequest
{
    public string? CustomAttribute1 { get; init; }
    public string? CustomAttribute2 { get; init; }
    public string? CustomAttribute3 { get; init; }
    public string? CustomAttribute4 { get; init; }
    public string? CustomAttribute5 { get; init; }
}

