using BT.Domain.Features.IAM.Permissions.Entities;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;

namespace BT.Application.Features.IAM.Permissions.Mappings;

public static class PermissionMapping
{
    public static PermissionResponse ToPermissionResponse(this Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        return new PermissionResponse(
            permission.Id,
            permission.DepartmentId,
            permission.Key,
            permission.Context,
            permission.Resource,
            permission.Action,
            permission.Description,
            permission.IsActive, permission.AvailableTenantId, permission.RequiredModule, permission.IsTenantDelegatable);
    }
}
