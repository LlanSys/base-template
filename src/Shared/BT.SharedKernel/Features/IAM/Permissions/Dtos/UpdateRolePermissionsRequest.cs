namespace BT.SharedKernel.Features.IAM.Permissions.Dtos;

public sealed record UpdateRolePermissionsRequest(
    IReadOnlyList<string> PermissionKeys,
    IReadOnlyList<string>? OriginalPermissionKeys = null);
