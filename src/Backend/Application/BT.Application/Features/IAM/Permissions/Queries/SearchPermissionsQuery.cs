using System.Collections.ObjectModel;
using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.IAM.Permissions.Mappings;
using BT.Application.Utilities;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Features.IAM.Permissions.Entities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Application.Features.IAM.Permissions.Queries;


public sealed record SearchPermissionsQuery(PermissionSearchRequest SearchRequest, string UserId)
    : IRequest<AppResponse<PagedResponse<PermissionResponse, Guid>>>, ICachableRequest
{
    public string CacheGroup => "permissions";

    public string Discriminator => CacheKeys.Discriminator(SearchRequest);

    public string? CacheUserId => UserId;

    public bool IsVersioned => true;
    public bool BypassCache => true;
}

