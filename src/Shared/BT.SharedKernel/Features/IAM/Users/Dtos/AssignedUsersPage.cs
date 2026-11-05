namespace BT.SharedKernel.Features.IAM.Users.Dtos;

public sealed record AssignedUsersPage(IReadOnlyList<AssignedUserResponse> Items, string? NextCursor);
