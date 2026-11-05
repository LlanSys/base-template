using Asp.Versioning;
using BT.Api.Common.Authorization;
using BT.Api.Common.Controllers;
using BT.Application.Features.Shared.OrgSettings.CommandHandlers;
using BT.Application.Features.Shared.OrgSettings.QueryHandlers;
using BT.SharedKernel.Features.Shared.OrgSettings.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BT.Api.Features.Shared.OrgSettings.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shared/tenant-settings")]
[ApiController]
[Authorize]
public sealed class OrgSettingsController(ISender sender) : BaseController
{
    [HttpGet]
    [RequirePermission("tenant-settings.view")]
    public async Task<IActionResult> GetOrgSettings(CancellationToken ct)
    {
        var response = await sender.Send(new GetOrgSettingsQuery(), ct).ConfigureAwait(false);
        return HandleResponse(response);
    }

    [HttpGet("{key}")]
    [RequirePermission("tenant-settings.view")]
    public async Task<IActionResult> GetOrgSettingByKey(string key, CancellationToken ct)
    {
        var response = await sender.Send(new GetOrgSettingByKeyQuery(key), ct).ConfigureAwait(false);
        return HandleResponse(response);
    }

    [HttpPost]
    [RequirePermission("tenant-settings.update")]
    public async Task<IActionResult> CreateOrgSetting([FromBody] CreateOrgSettingRequest request, CancellationToken ct)
    {
        var response = await sender.Send(new CreateOrgSettingCommand(request, GetUserId()), ct).ConfigureAwait(false);
        return HandleResponse(response);
    }

    [HttpPut]
    [RequirePermission("tenant-settings.update")]
    public async Task<IActionResult> UpdateOrgSetting([FromBody] UpdateOrgSettingRequest request, CancellationToken ct)
    {
        var response = await sender.Send(new UpdateOrgSettingCommand(request, GetUserId()), ct).ConfigureAwait(false);
        return HandleResponse(response);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("tenant-settings.update")]
    public async Task<IActionResult> DeleteOrgSetting(Guid id, CancellationToken ct)
    {
        var response = await sender.Send(new DeleteOrgSettingCommand(id, GetUserId()), ct).ConfigureAwait(false);
        return HandleResponse(response);
    }

    private string GetUserId()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("User context is not available.");
        }
        return userId;
    }
}
