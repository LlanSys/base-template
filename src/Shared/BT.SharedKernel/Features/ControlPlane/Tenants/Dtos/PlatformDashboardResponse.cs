namespace BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record PlatformDashboardResponse(int Tenants, int Active, int Suspended, int PendingKyc, int Provisioning, int ProvisioningFailed,
    int? InvitationsPending, int? InvitationsFailed, int? ActiveStamps, int? DisabledStamps);
