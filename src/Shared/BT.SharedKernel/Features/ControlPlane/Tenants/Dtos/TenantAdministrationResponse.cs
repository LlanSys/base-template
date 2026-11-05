using BT.SharedKernel.Features.IAM.Users.Dtos;
namespace BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record TenantAdministrationResponse(IReadOnlyList<TenantModuleStatusResponse> Modules, IReadOnlyList<TenantInvitationResponse> Invitations, IReadOnlyList<AssignedUserResponse> Administrators);
