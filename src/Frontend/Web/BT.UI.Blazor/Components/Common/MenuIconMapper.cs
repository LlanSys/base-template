using BT.SharedKernel.Features.IAM.Menus;

using MudBlazor;

using System.Reflection;

namespace BT.UI.Blazor.Components.Common;

internal static class MenuIconMapper
{
    private static readonly Lazy<Dictionary<string, string>> MaterialFilledIcons = new(BuildMaterialFilledIconMap);

    public static IEnumerable<string> Keys => MenuIconCatalog.Keys.Where(IsSupported);

    public static bool IsSupported(string? key)
    {
        return MaterialFilledIcons.Value.ContainsKey(key?.Trim() ?? string.Empty);
    }

    public static string Resolve(string? key)
    {
        return MaterialFilledIcons.Value.GetValueOrDefault(key?.Trim() ?? string.Empty, Icons.Material.Filled.Menu);
    }

    private static Dictionary<string, string> BuildMaterialFilledIconMap()
    {
        return typeof(Icons.Material.Filled)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(static field => field.IsLiteral && field.FieldType == typeof(string))
            .OrderBy(static field => field.Name, StringComparer.Ordinal)
            .GroupBy(static field => field.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .ToDictionary(
                static field => field.Name,
                static field => (string)field.GetRawConstantValue()!,
                StringComparer.OrdinalIgnoreCase);
    }
}
