namespace BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

public sealed record AdministratorInvitationPreview(string OrganizationName, string Email, string FirstName, bool ExistingAccount, DateTimeOffset ExpiresAt);
