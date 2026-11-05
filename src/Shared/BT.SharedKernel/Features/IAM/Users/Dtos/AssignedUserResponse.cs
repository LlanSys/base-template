namespace BT.SharedKernel.Features.IAM.Users.Dtos;

public sealed record AssignedUserResponse(string Id, string Name, string Email, bool IsActive);
