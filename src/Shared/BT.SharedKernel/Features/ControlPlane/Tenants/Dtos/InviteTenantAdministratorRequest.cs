namespace BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record InviteTenantAdministratorRequest(string Email, string FirstName, string LastName);
