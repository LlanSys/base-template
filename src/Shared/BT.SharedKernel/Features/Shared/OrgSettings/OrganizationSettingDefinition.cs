namespace BT.SharedKernel.Features.Shared.OrgSettings;

public sealed record OrganizationSettingDefinition(string Key, string Label, string Description, string DefaultValue, string Kind);
