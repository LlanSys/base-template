using System.Globalization;

namespace BT.SharedKernel.Features.Shared.OrgSettings;

public static class OrganizationFormatting
{
    public static string FormatDate(DateTimeOffset instant, OrganizationPreferences? preferences)
    {
        preferences ??= OrganizationSettingCatalog.Defaults;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(preferences.TimeZoneId);
        return TimeZoneInfo.ConvertTime(instant, zone).ToString(preferences.DateFormat + " HH:mm", CultureInfo.GetCultureInfo(preferences.CultureName));
    }

    public static string FormatCount(int count, OrganizationPreferences? preferences)
        => count.ToString("N0", CultureInfo.GetCultureInfo((preferences ?? OrganizationSettingCatalog.Defaults).CultureName));
}
