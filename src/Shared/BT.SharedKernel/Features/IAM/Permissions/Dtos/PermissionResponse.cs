namespace BT.SharedKernel.Features.IAM.Permissions.Dtos;

public sealed record PermissionResponse(
    Guid Id,
    Guid? DepartmentId,
    string Key,
    string Context,
    string Resource,
    string Action,
    string Description,
    bool IsActive,
    Guid? AvailableTenantId = null,
    string? RequiredModule = null,
    bool IsTenantDelegatable = false)
{
    public bool IsProtected => BT.SharedKernel.Features.IAM.Permissions.ImplementedPermissions.IsProtected(Key);
}
