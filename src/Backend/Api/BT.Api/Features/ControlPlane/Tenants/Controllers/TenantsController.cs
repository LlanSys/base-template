using Asp.Versioning;

using BT.Api.Common.Authorization;
using BT.Api.Common.Controllers;
using BT.Api.Features.ControlPlane.Tenants.Dtos;
using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Application.Features.ControlPlane.Tenants.Queries;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BT.Api.Features.ControlPlane.Tenants.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/control-plane/tenants")]
[ApiController]
[Authorize]
public sealed class TenantsController(ISender sender) : BaseController
{
    [HttpPost("{id:guid}/administrator")]
    [RequirePermission("controlplane.manage_tenant_administrators")]
    public async Task<IActionResult> InviteAdministrator(Guid id, InviteTenantAdministratorRequest request, CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new InviteTenantAdministratorCommand(id, request,
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value), cancellationToken).ConfigureAwait(false));
    [HttpGet("dashboard")]
    [RequirePermission("controlplane.view_tenants")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new GetPlatformDashboardQuery(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value), cancellationToken).ConfigureAwait(false));

    [HttpGet]
    [RequirePermission("controlplane.view_tenants")]
    public async Task<IActionResult> GetAllTenants()
        => HandleResponse(await sender.Send(new GetAllTenantsQuery()).ConfigureAwait(false));

    [HttpPost("{id:guid}/administrator-invitations/{invitationId:guid}/retry")]
    [RequirePermission("controlplane.manage_tenant_administrators")]
    public async Task<IActionResult> RetryInvitation(Guid id, Guid invitationId, CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new RetryAdministratorInvitationCommand(id, invitationId), cancellationToken).ConfigureAwait(false));

    [HttpGet("{id:guid}/administration")]
    [RequirePermission("controlplane.view_tenants")]
    public async Task<IActionResult> GetAdministration(Guid id, CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new GetTenantAdministrationQuery(id, User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value), cancellationToken).ConfigureAwait(false));

    [HttpPost("{id:guid}/administrator-invitations/{invitationId:guid}/revoke")]
    [RequirePermission("controlplane.manage_tenant_administrators")]
    public async Task<IActionResult> RevokeInvitation(Guid id, Guid invitationId, CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new RevokeAdministratorInvitationCommand(id, invitationId), cancellationToken).ConfigureAwait(false));

    [HttpGet("{id:guid}")]
    [RequirePermission("controlplane.view_tenants")]
    public async Task<IActionResult> GetTenantById(Guid id)
        => HandleResponse(await sender.Send(new GetTenantByIdQuery(id)).ConfigureAwait(false));

    [HttpPost]
    [RequirePermission("controlplane.create_tenants")]
    public async Task<IActionResult> CreateTenant(CreateTenantRequest request)
    {
        var response = await sender.Send(new CreateTenantCommand(request)).ConfigureAwait(false);
        return HandleResponse(response, onSuccess: r =>
        {
            if (string.Equals(r.Data?.Status, "Provisioning", StringComparison.OrdinalIgnoreCase))
            {
                return Accepted(r);
            }

            return Ok(r);
        });
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("controlplane.edit_tenants")]
    public async Task<IActionResult> UpdateTenant(Guid id, UpdateTenantRequest request)
        => HandleResponse(await sender.Send(new UpdateTenantCommand(id, request)).ConfigureAwait(false));

    [HttpDelete("{id:guid}")]
    [RequirePermission("controlplane.delete_tenants")]
    public async Task<IActionResult> DeleteTenant(Guid id, CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new DeleteTenantCommand(id), cancellationToken).ConfigureAwait(false));

    [HttpPut("{id:guid}/modules")]
    [RequirePermission("controlplane.manage_tenant_modules")]
    public async Task<IActionResult> SetModules(Guid id, SetTenantModulesRequest request, CancellationToken cancellationToken)
        => HandleResponse(await sender.Send(new SetTenantModulesCommand(id, request), cancellationToken).ConfigureAwait(false));

    [HttpPost("{id:guid}/activate")]
    [RequirePermission("controlplane.manage_tenant_lifecycle")]
    public async Task<IActionResult> ActivateTenant(Guid id)
        => HandleResponse(await sender.Send(new ActivateTenantCommand(id)).ConfigureAwait(false));

    [HttpPost("{id:guid}/suspend")]
    [RequirePermission("controlplane.manage_tenant_lifecycle")]
    public async Task<IActionResult> SuspendTenant(Guid id)
        => HandleResponse(await sender.Send(new SuspendTenantCommand(id)).ConfigureAwait(false));

    [HttpPost("{id:guid}/approve-kyc")]
    [RequirePermission("controlplane.manage_tenant_lifecycle")]
    public async Task<IActionResult> ApproveKYC(Guid id)
        => HandleResponse(await sender.Send(new ApproveTenantKYCCommand(id)).ConfigureAwait(false));

    [HttpPost("{id:guid}/modules")]
    [RequirePermission("controlplane.manage_tenant_modules")]
    public async Task<IActionResult> AddModule(Guid id, AddTenantModuleRequest request)
        => HandleResponse(await sender.Send(new AddTenantModuleCommand(id, request)).ConfigureAwait(false));

    [HttpDelete("{id:guid}/modules/{moduleKey}")]
    [RequirePermission("controlplane.manage_tenant_modules")]
    public async Task<IActionResult> RemoveModule(Guid id, string moduleKey)
        => HandleResponse(await sender.Send(new RemoveTenantModuleCommand(id, new RemoveTenantModuleRequest { ModuleKey = moduleKey })).ConfigureAwait(false));

    [HttpPost("{id:guid}/stamp-migration")]
    [RequirePermission("controlplane.migrate_tenants")]
    public async Task<IActionResult> MigrateStamp(Guid id, MigrateTenantStampRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return HandleResponse(await sender.Send(new MigrateTenantStampCommand(id, request.NewDeploymentStampId, request.NewDatabaseConnectionString)).ConfigureAwait(false));
    }
}
