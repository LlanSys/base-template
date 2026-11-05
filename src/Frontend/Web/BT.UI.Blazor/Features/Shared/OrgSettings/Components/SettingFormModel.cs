namespace BT.UI.Blazor.Features.Shared.OrgSettings.Components;

internal sealed class SettingFormModel
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
