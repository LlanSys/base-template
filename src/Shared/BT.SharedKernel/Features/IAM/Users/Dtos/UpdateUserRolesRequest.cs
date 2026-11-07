namespace BT.SharedKernel.Features.IAM.Users.Dtos;

public sealed record UpdateUserRolesRequest(
    IReadOnlyList<string> Roles,
    IReadOnlyList<string>? OriginalRoles = null);
