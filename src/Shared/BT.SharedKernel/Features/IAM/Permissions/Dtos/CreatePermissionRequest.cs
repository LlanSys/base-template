namespace BT.SharedKernel.Features.IAM.Permissions.Dtos;

public sealed record CreatePermissionRequest
{
    public Guid? DepartmentId { get; init; }
    public Guid? AvailableTenantId { get; init; }
    public string? RequiredModule { get; init; }
    public bool IsTenantDelegatable { get; init; }

    public required string Context { get; init; }

    public required string Resource { get; init; }

    public required string Action { get; init; }

    public string Description { get; init; } = string.Empty;
}
