using System.Security.Claims;
using Asp.Versioning;
using BT.Api.Common.Authorization;
using BT.Api.Common.Controllers;
using BT.Application.Features.ControlPlane.Modules;
using BT.SharedKernel.Features.ControlPlane.Modules;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BT.Api.Features.ControlPlane.Modules;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{version:apiVersion}/control-plane/modules")]
[Microsoft.AspNetCore.Authorization.Authorize]
public sealed class ModulesController(ISender sender) : BaseController
{
    [HttpGet]
    [RequirePermission("modules.view")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new GetModulesQuery(), cancellationToken).ConfigureAwait(false));

    [HttpPost]
    [RequirePermission("modules.create")]
    public async Task<IActionResult> Create(ModuleDefinitionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IsActive && !User.HasClaim("permission", "modules.manage")) return Forbid();
        return HandleResponse(await sender.Send(new SaveModuleCommand(null, request, User.FindFirstValue(ClaimTypes.NameIdentifier)!), cancellationToken).ConfigureAwait(false));
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("modules.manage")]
    public async Task<IActionResult> Update(Guid id, ModuleDefinitionRequest request, CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new SaveModuleCommand(id, request, User.FindFirstValue(ClaimTypes.NameIdentifier)!), cancellationToken).ConfigureAwait(false));

    [HttpDelete("{id:guid}")]
    [RequirePermission("modules.manage")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new DeleteModuleCommand(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!), cancellationToken).ConfigureAwait(false));
}
