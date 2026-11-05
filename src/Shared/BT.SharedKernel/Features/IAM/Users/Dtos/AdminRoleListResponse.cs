namespace BT.SharedKernel.Features.IAM.Users.Dtos;

public sealed record AdminRoleListResponse(
    string Id,
    string Name,
    string NormalizedName,
    Guid? DepartmentId,
    string DepartmentName,
    int UserCount,
    bool IsDefault = false,
    bool IsActive = true,
    bool IsPlatformRole = false,
    string? ModuleKey = null);
