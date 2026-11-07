namespace BT.SharedKernel.Features.IAM.Users.Dtos;

public sealed record UpdateUserPermissionsRequest(
    IReadOnlyList<string> PermissionKeys,
    IReadOnlyList<string>? OriginalPermissionKeys = null);
