using System;
using System.Threading.Tasks;
using BT.Application.Features.ControlPlane.Tenants.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BT.Api.Common.Authorization;
using BT.Api.Features.ControlPlane.Tenants.Dtos;
using Asp.Versioning;

namespace BT.Api.Features.ControlPlane.Tenants.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/controlplane/webhooks/provisioning-completed")]
public class ProvisioningCallbackController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProvisioningCallbackController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{tenantId:guid}")]
    [RequirePermission("controlplane.manage_tenant_lifecycle")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteProvisioning(
        [FromRoute] Guid tenantId,
        [FromBody] CompleteTenantProvisioningRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var command = new CompleteTenantProvisioningCommand(
            tenantId,
            request.DatabaseConnectionString,
            request.ApplicationInsightsKey);

        var result = await _mediator.Send(command).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
