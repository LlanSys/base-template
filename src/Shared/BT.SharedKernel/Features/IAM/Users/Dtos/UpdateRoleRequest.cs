namespace BT.SharedKernel.Features.IAM.Users.Dtos;

public sealed record UpdateRoleRequest(string Name, Guid? DepartmentId = null, bool IsActive = true);
