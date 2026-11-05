namespace BT.SharedKernel.Features.IAM.Permissions.Dtos;

public sealed record RolePermissionUsageResponse(string RoleId, string RoleName, string Scope, bool IsDefault,
    bool IsPlatformRole, bool HasPermission, Guid? DepartmentId);
