namespace BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public class UpdateTenantRequest
{
    public Guid Revision { get; set; }
    public required string DisplayName { get; set; }
    public required string HostName { get; set; }
    public string? ContactEmail { get; set; }
    public int MaxUsers { get; set; }
    public required string SubscriptionTier { get; set; }

    public string? DatabaseProvider { get; set; }
    public string? DatabaseConnectionString { get; set; }
}
