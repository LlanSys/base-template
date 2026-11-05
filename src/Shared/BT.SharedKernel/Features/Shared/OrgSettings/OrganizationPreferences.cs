namespace BT.SharedKernel.Features.Shared.OrgSettings;

public sealed record OrganizationPreferences(string Name, string TimeZoneId, string CultureName, string DateFormat);
