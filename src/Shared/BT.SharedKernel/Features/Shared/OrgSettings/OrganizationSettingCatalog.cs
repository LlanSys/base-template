using System.Globalization;

namespace BT.SharedKernel.Features.Shared.OrgSettings;

public static class OrganizationSettingCatalog
{
    public static IReadOnlyList<OrganizationSettingDefinition> Definitions { get; } = Array.AsReadOnly(new[]
    {
        new OrganizationSettingDefinition("Organization.Name", "Organization name", "Shown in the application header.", "BaseTemplate", "Text"),
        new OrganizationSettingDefinition("Organization.TimeZone", "Time zone", "Used to display organization dates and times.", "UTC", "TimeZone"),
        new OrganizationSettingDefinition("Organization.Culture", "Regional format", "Used to format organization numbers and dates.", "en-KE", "Culture"),
        new OrganizationSettingDefinition("Organization.DateFormat", "Date format", "Used for organization date displays.", "dd/MM/yyyy", "DateFormat")
    });

    public static OrganizationSettingDefinition? Find(string key) => Definitions.FirstOrDefault(
        definition => string.Equals(definition.Key, key, StringComparison.Ordinal));

    public static bool IsValid(string key, string value)
    {
        var definition = Find(key);

        if (definition is null || string.IsNullOrWhiteSpace(value) || value.Length > 160) return false;
        return definition.Kind switch
        {
            "Text" => value.Length <= 120,
            "TimeZone" => TimeZoneInfo.TryFindSystemTimeZoneById(value, out _),
            "Culture" => CultureInfo.GetCultures(CultureTypes.SpecificCultures).Any(
                culture => string.Equals(culture.Name, value, StringComparison.Ordinal)),
            "DateFormat" => value is "dd/MM/yyyy" or "MM/dd/yyyy" or "yyyy-MM-dd",
            _ => false
        };
    }

    public static OrganizationPreferences Defaults => new("BaseTemplate", "UTC", "en-KE", "dd/MM/yyyy");
}
