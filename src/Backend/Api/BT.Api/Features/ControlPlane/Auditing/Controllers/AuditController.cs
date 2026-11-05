using Asp.Versioning;

using BT.Api.Common.Authorization;
using BT.Api.Common.Controllers;
using BT.Application.Features.ControlPlane.Auditing.Commands;
using BT.Application.Features.ControlPlane.Auditing.Queries;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Auditing.Dtos;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BT.Api.Features.ControlPlane.Auditing.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/control-plane/audit")]
[ApiController]
[Authorize]
public class AuditController(ISender sender) : BaseController
{
    [HttpGet("changes")]
    [RequirePermission("controlplane.view_audit")]
    public async Task<IActionResult> GetChanges([FromQuery] Guid? tenantId, CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new GetAdministrationAuditQuery(tenantId), cancellationToken).ConfigureAwait(false));
    [HttpPost("impersonate/start")]
    [RequirePermission("controlplane.impersonate")]
    [ProducesResponseType(typeof(AppResponse<ImpersonationRecordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> StartImpersonation(
        [FromBody] StartImpersonationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var command = new StartImpersonationCommand(request.TargetTenantId, request.Reason, request.DurationHours);
        return HandleResponse(await sender.Send(command, cancellationToken).ConfigureAwait(false));
    }

    [HttpPost("impersonate/end")]
    [RequirePermission("controlplane.impersonate")]
    [ProducesResponseType(typeof(AppResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EndImpersonation(
        [FromBody] EndImpersonationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var command = new EndImpersonationCommand(request.ImpersonationRecordId);
        return HandleResponse(await sender.Send(command, cancellationToken).ConfigureAwait(false));
    }

    [HttpGet("impersonate/records")]
    [RequirePermission("controlplane.impersonate")]
    [ProducesResponseType(typeof(AppResponse<IReadOnlyList<ImpersonationRecordDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetImpersonationRecords(
        [FromQuery] bool activeOnly,
        CancellationToken cancellationToken)
    {
        return HandleResponse(await sender.Send(new GetImpersonationRecordsQuery(activeOnly), cancellationToken).ConfigureAwait(false));
    }
}
