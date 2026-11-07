namespace BT.SharedKernel.Features.IAM.Users.Dtos;

public sealed record UserRoleOption(string Id, string Name, string? ModuleKey, bool IsActive, bool CanAssign);
