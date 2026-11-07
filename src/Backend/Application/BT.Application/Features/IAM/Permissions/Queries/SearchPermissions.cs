using System.Collections.ObjectModel;
using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.IAM.Permissions.Mappings;
using BT.Application.Utilities;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Features.IAM.Permissions.Entities;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.IAM.Permissions.Dtos;
using BT.SharedKernel.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BT.Application.Features.IAM.Permissions.Queries;

internal sealed class SearchPermissionsQueryHandler(
    IIamUnitOfWork unitOfWork,
    ILogger<SearchPermissionsQueryHandler> logger,
    Authorization.IEffectiveAccessService accessService)
    : IRequestHandler<SearchPermissionsQuery, AppResponse<PagedResponse<PermissionResponse, Guid>>>
{
    public async Task<AppResponse<PagedResponse<PermissionResponse, Guid>>> Handle(SearchPermissionsQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var request = query.SearchRequest;
            var pageSize = Math.Clamp(request.PageSize, 1, 50);
            var access = await accessService.GetAsync(query.UserId, cancellationToken).ConfigureAwait(false);
            var allowedKeys = access.Permissions.ToArray();

            IQueryable<Permission> ApplyFilters(IQueryable<Permission> permissions)
            {
                if (!access.IsPlatformStaff)
                {
                    permissions = permissions.Where(permission => allowedKeys.Contains(permission.Key));
                }

                if (!string.IsNullOrWhiteSpace(request.GlobalSearch))
                {
                    var search = request.GlobalSearch.Trim();
                    permissions = permissions.Where(permission =>
                        permission.Key.Contains(search)
                        || permission.Context.Contains(search)
                        || permission.Resource.Contains(search)
                        || permission.Action.Contains(search)
                        || permission.Description.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(request.Context))
                {
                    permissions = permissions.Where(permission => permission.Context == request.Context);
                }

                if (!string.IsNullOrWhiteSpace(request.Resource))
                {
                    permissions = permissions.Where(permission => permission.Resource == request.Resource);
                }

                if (request.DepartmentId.HasValue)
                {
                    permissions = permissions.Where(permission => permission.DepartmentId == null || permission.DepartmentId == request.DepartmentId.Value);
                }

                if (request.IsActive.HasValue)
                {
                    permissions = permissions.Where(permission => permission.IsActive == request.IsActive.Value);
                }

                return permissions;
            }

            var totalCount = await unitOfWork.PermissionRepository.CountAsync(ApplyFilters, cancellationToken).ConfigureAwait(false);

            IQueryable<Permission> ApplyPaging(IQueryable<Permission> permissions)
            {
                permissions = ApplyFilters(permissions);

                if (request.Cursor.HasValue && request.Cursor != Guid.Empty)
                {
                    permissions = permissions.Where(permission => permission.Id.CompareTo(request.Cursor.Value) > 0);
                }

                return permissions
                    .OrderBy(static permission => permission.Id)
                    .Take(pageSize + 1);
            }

            var page = await unitOfWork.PermissionRepository.ListAsync(ApplyPaging, cancellationToken).ConfigureAwait(false);

            var hasNextPage = page.Count > pageSize;

            if (hasNextPage)
            {
                page.RemoveAt(page.Count - 1);
            }

            var items = page
                .Select(static permission => permission.ToPermissionResponse())
                .OrderBy(static permission => permission.Context, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static permission => permission.Resource, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static permission => permission.Action, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var nextCursor = hasNextPage ? page[^1].Id : (Guid?)null;
            var result = new PagedResponse<PermissionResponse, Guid>(
                new Collection<PermissionResponse>(items),
                totalCount,
                1,
                pageSize,
                request.Cursor is null || request.Cursor == Guid.Empty,
                nextCursor ?? Guid.Empty)
            {
                HasNextPage = hasNextPage,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return AppResponses.Success(result);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogPipelineException(logger, nameof(SearchPermissionsQueryHandler), ex);
            throw;
        }
    }
}
