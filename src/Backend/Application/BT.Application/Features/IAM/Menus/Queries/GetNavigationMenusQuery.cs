using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.IAM.Menus.Mappings;
using BT.Application.Utilities;
using BT.Domain.Features.IAM.Contracts;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Menus.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Application.Features.IAM.Menus.Queries;


public sealed record GetNavigationMenusQuery(string Placement, IReadOnlyList<string> PermissionKeys, string UserId, bool HasFullAccess = false)
    : IRequest<AppResponse<IReadOnlyList<MenuResponse>>>, ICachableRequest
{
    public string CacheGroup => "menus";
    public string Discriminator => CacheKeys.Discriminator(new { Placement, HasFullAccess, Permissions = PermissionKeys.Order(StringComparer.OrdinalIgnoreCase) });
    public string? CacheUserId => UserId;
    public bool IsVersioned => true;
    public bool BypassCache => true;
}

