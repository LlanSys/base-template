using BT.Application.Features.ControlPlane.Auditing.Queries;
using BT.Domain.Shared.Entities;
using BT.Persistence.Features.ControlPlane.DataContext;
using BT.Persistence.Features.IAM.DataContext;
using BT.Persistence.Features.Shared.DataContext;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Auditing.Dtos;

using MediatR;

using Microsoft.EntityFrameworkCore;
namespace BT.Infrastructure.Features.ControlPlane.Auditing.QueryHandlers;

internal sealed class GetAdministrationAuditQueryHandler(ControlPlaneDBContext controlPlane, IamDBContext iam, SharedDBContext shared)
    : IRequestHandler<GetAdministrationAuditQuery, AppResponse<IReadOnlyList<AdministrationAuditResponse>>>
{
    public async Task<AppResponse<IReadOnlyList<AdministrationAuditResponse>>> Handle(GetAdministrationAuditQuery request, CancellationToken cancellationToken)
    {
        var entries = new List<AdministrationAuditEntry>();
        var tenantKey = request.TenantId.HasValue ? request.TenantId.Value.ToString() : string.Empty;
        var tenantUsers = iam.Users.IgnoreQueryFilters().Where(user => user.TenantId == request.TenantId).Select(user => user.Id);
        var tenantRoles = iam.Roles.IgnoreQueryFilters().Where(role => !role.IsDefault && role.TenantId == request.TenantId).Select(role => role.Id);
        entries.AddRange(await controlPlane.Set<AdministrationAuditEntry>().AsNoTracking().Where(entry => request.TenantId == null || entry.TenantId == request.TenantId || entry.EntityType == "Tenant" && entry.EntityId == tenantKey).OrderByDescending(entry => entry.OccurredAt).Take(100).ToListAsync(cancellationToken).ConfigureAwait(false));
        entries.AddRange(await iam.Set<AdministrationAuditEntry>().IgnoreQueryFilters().AsNoTracking().Where(entry => request.TenantId == null || entry.TenantId == request.TenantId || tenantUsers.Contains(entry.EntityId) || tenantRoles.Contains(entry.EntityId)).OrderByDescending(entry => entry.OccurredAt).Take(100).ToListAsync(cancellationToken).ConfigureAwait(false));
        entries.AddRange(await shared.Set<AdministrationAuditEntry>().IgnoreQueryFilters().AsNoTracking().Where(entry => request.TenantId == null || entry.TenantId == request.TenantId).OrderByDescending(entry => entry.OccurredAt).Take(100).ToListAsync(cancellationToken).ConfigureAwait(false));
        var latest = entries.OrderByDescending(entry => entry.OccurredAt).Take(100).ToArray();
        var ids = latest.Select(entry => entry.ActorId).Distinct().ToArray();
        var actors = await iam.Users.IgnoreQueryFilters().AsNoTracking().Where(user => ids.Contains(user.Id))
            .Select(user => new { user.Id, Name = user.FirstName + " " + user.LastName })
            .ToDictionaryAsync(user => user.Id, user => user.Name, cancellationToken).ConfigureAwait(false);

        var result = latest.Select(entry => new AdministrationAuditResponse(entry.Id, entry.OccurredAt,
            actors.GetValueOrDefault(entry.ActorId, "System or former account"), Area(entry.EntityType), entry.Action, entry.ChangedFields)).ToArray();

        return AppResponses.Success<IReadOnlyList<AdministrationAuditResponse>>(result);
    }
    private static string Area(string type) => type switch
    {
        "AppRole" => "Roles",
        "AppUser" => "Users",
        "Permission" => "Permissions",
        "MenuItem" => "Menus",
        "IdentityUserRole<string>" => "User roles",
        "IdentityRoleClaim<string>" => "Role permissions",
        "IdentityUserClaim<string>" => "User permissions",
        "OrgSetting" => "Organization settings",
        "ModuleDefinition" => "Modules",
        "TenantModule" => "Tenant modules",
        "DeploymentStamp" => "Deployment stamps",
        "Tenant" => "Tenants",
        _ => "Administration"
    };
}
