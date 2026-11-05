using System.ComponentModel.DataAnnotations;

namespace BT.SharedKernel.Features.IAM.Users.Dtos;

public sealed record CreateRoleRequest([Required, StringLength(80)] string Name, Guid? DepartmentId = null,
    string? ModuleKey = null, bool IsDefault = false);
