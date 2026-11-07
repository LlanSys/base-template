using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.IAM.Menus.Mappings;
using BT.Application.Utilities;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Features.IAM.Menus.Entities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Menus.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Application.Features.IAM.Menus.Commands;

internal sealed class CreateMenuCommandHandler(IIamUnitOfWork unitOfWork, ILogger<CreateMenuCommandHandler> logger)
    : IRequestHandler<CreateMenuCommand, AppResponse<MenuResponse>>
{
    public async Task<AppResponse<MenuResponse>> Handle(CreateMenuCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var request = command.Request;

            if (!BT.SharedKernel.Features.IAM.Menus.ImplementedMenuRoutes.Matches(request.Url, request.RequiredPermissionKey, request.RequiredModule))
            {
                return AppResponses.Failure<MenuResponse>("Select an implemented route with its registered permission and business module.");
            }

            var catalogError = await ValidateCatalogAsync(unitOfWork, request.Placement, request.Icon, request.Url, request.RequiredPermissionKey, cancellationToken)
                .ConfigureAwait(false);

            if (catalogError is not null)
            {
                return AppResponses.Failure<MenuResponse>(catalogError);
            }

            if (request.ParentId.HasValue)
            {
                var parent = await unitOfWork.MenuRepository.FindByIdAsync(request.ParentId.Value, cancellationToken).ConfigureAwait(false);

                if (parent is null)
                {
                    return AppResponses.Failure<MenuResponse>("Parent menu not found.");
                }

                if (!string.Equals(parent.Placement, request.Placement, StringComparison.OrdinalIgnoreCase)
                    || parent.DepartmentId != request.DepartmentId)
                {
                    return AppResponses.Failure<MenuResponse>("Parent menu must use the same placement and department scope.");
                }
            }

            var key = Slugify(request.Title);
            var menu = MenuItem.Create(request.ParentId, request.DepartmentId, key, request.Title, request.Description, request.Url, request.Icon, request.Placement, request.RequiredPermissionKey, request.RequiredModule, request.DisplayOrder, command.UserId);
            var duplicate = await unitOfWork.MenuRepository
                .AnyAsync(existing => existing.Key == menu.Key, cancellationToken)
                .ConfigureAwait(false);

            if (duplicate)
            {
                return AppResponses.Failure<MenuResponse>($"Menu key {menu.Key} already exists.");
            }

            menu.AvailableTenantId = request.AvailableTenantId;
            await unitOfWork.MenuRepository.CreateAsync(menu, cancellationToken).ConfigureAwait(false);
            var saved = await unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false) > 0;

            return saved ? AppResponses.Success("Menu created.", menu.ToMenuResponse()) : AppResponses.Failure<MenuResponse>("Menu create failed.");
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(CreateMenuCommandHandler), ex);
            throw;
        }
    }

    private static string Slugify(string value)
    {
        return string.Join('-', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static async Task<string?> ValidateCatalogAsync(
        IIamUnitOfWork unitOfWork,
        string placement,
        string icon,
        string url,
        string? requiredPermissionKey,
        CancellationToken cancellationToken)
    {
        var normalizedPlacement = placement.Trim();

        var placementExists = await unitOfWork.MenuPlacementRepository
            .AnyAsync(item => item.IsActive && item.Key == normalizedPlacement, cancellationToken)
            .ConfigureAwait(false);

        if (!placementExists)
        {
            return $"Menu placement '{placement}' is not registered.";
        }

        if (!await MenuIconAvailability.IsAvailableAsync(unitOfWork, icon, cancellationToken).ConfigureAwait(false))
        {
            return $"Menu icon '{icon}' is unavailable.";
        }

        var routeExists = string.IsNullOrWhiteSpace(url)
            || await unitOfWork.MenuRouteRepository
                .AnyAsync(item => item.IsActive && item.Url == url.Trim() && item.PlacementKey == normalizedPlacement, cancellationToken)
                .ConfigureAwait(false);

        if (!routeExists)
        {
            return $"Menu route '{url}' is not registered for placement '{placement}'.";
        }

        if (string.IsNullOrWhiteSpace(requiredPermissionKey))
        {
            return null;
        }

        var normalizedPermissionKey = requiredPermissionKey.Trim().ToLowerInvariant();
        var permissionExists = await unitOfWork.PermissionRepository
            .AnyAsync(item => item.IsActive && item.Key == normalizedPermissionKey, cancellationToken)
            .ConfigureAwait(false);

        return permissionExists ? null : $"Required permission '{requiredPermissionKey}' is not registered.";
    }
}
