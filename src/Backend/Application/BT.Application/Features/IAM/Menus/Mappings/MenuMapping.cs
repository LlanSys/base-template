using BT.Domain.Features.IAM.Menus.Entities;
using BT.SharedKernel.Features.IAM.Menus.Dtos;

namespace BT.Application.Features.IAM.Menus.Mappings;

public static class MenuMapping
{
    public static MenuResponse ToMenuResponse(this MenuItem menu, IReadOnlyList<MenuResponse>? children = null)
    {
        ArgumentNullException.ThrowIfNull(menu);

        return new MenuResponse(
            menu.Id,
            menu.ParentId,
            menu.DepartmentId,
            menu.Key,
            menu.Title,
            menu.Description,
            menu.Url,
            menu.Icon,
            menu.Placement,
            menu.RequiredPermissionKey,
            menu.RequiredModule,
            menu.DisplayOrder,
            menu.IsActive,
            children ?? [], menu.AvailableTenantId);
    }

    public static IReadOnlyList<MenuResponse> ToTree(this IReadOnlyList<MenuItem> menus)
    {
        var lookup = menus.ToLookup(static menu => menu.ParentId);

        return Build(null);

        List<MenuResponse> Build(Guid? parentId)
            => [.. lookup[parentId]
                .OrderBy(static menu => menu.DisplayOrder).ThenBy(static menu => menu.Title)
                .Select(child => child.ToMenuResponse(Build(child.Id)))
                .Where(child => !string.IsNullOrWhiteSpace(child.Url) || child.Children.Count > 0)];
    }
}
