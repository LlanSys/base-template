using BT.Application.Contracts.Interfaces.Common;
using BT.SharedKernel.Extensions;
using BT.Application.Features.Banking.Customers.Mappings;
using BT.Application.Features.HR.Employees.Mappings;
using BT.Application.Features.IAM.Users.Mappings;
using BT.Application.Features.Shared.EmailTemplates.Mappings;
using BT.Application.Utilities;
using BT.Domain.Features.Banking.Customers.Contracts.Specifications;
using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.Contracts.Specifications;
using BT.Domain.Features.Banking.Customers.Entities;
using BT.SharedKernel.Features.Banking.Customers.Dtos;
using BT.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;

namespace BT.Application.Features.Banking.Customers.QueryHandlers;


// ════════════════════════════════════════════════════════════════════════════════
//  GET CUSTOMER LIST  (unfiltered, paginated)
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Returns the full customer list with cursor-based pagination, no search filters.
///
/// Cache strategy — VERSIONED list entry:
///   Key:  "customers:list:global:{versionToken}:{discriminator}"
///   TTL:  30 minutes
///   Scope: global — the handler currently returns the same data for every user.
///
/// Invalidation: any mutation command bumps CacheKeys.GroupVersion("customers"),
/// which orphans every versioned entry for that user in O(1).
/// </summary>


internal sealed class GetCustomerListQueryHandler(IBankingUnitOfWork _bankingUnitOfWork, ILogger<GetCustomerListQueryHandler> _logger)
    : IRequestHandler<GetCustomerListQuery, AppResponse<PagedResponse<CustomerResponse, Guid>>>
{
    public async Task<AppResponse<PagedResponse<CustomerResponse, Guid>>> Handle(GetCustomerListQuery query, CancellationToken ct)
    {
        try
        {
            var req = query.CustomerListRequest;

            var pageSize = Math.Clamp(req.PageSize, 1, 50);

            var spec = new CustomerSearchSpec(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                req.Cursor,
                pageSize + 1);

            var totalCount = await _bankingUnitOfWork.CustomerRepository.CountAsync(ct).ConfigureAwait(false);
            var customerEntities = await _bankingUnitOfWork.CustomerRepository.SearchAsync(spec, ct).ConfigureAwait(false);

            var hasNextPage = customerEntities.Count > pageSize;

            if (hasNextPage)
                customerEntities.RemoveAt(customerEntities.Count - 1);

            var items = customerEntities.Select(c => c.ToCustomerResponse()).ToList();
            var nextCursor = hasNextPage ? items[^1].Id : (Guid?)null;

            bool isFirstPage = req.Cursor == null || req.Cursor == Guid.Empty;

            var pagedResult = new PagedResponse<CustomerResponse, Guid>(
                new Collection<CustomerResponse>(items),
                totalCount,
                1,
                pageSize,
                isFirstPage,
                nextCursor ?? Guid.Empty
            );

            return AppResponses.Success(pagedResult);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogCustomerListFetchFailed(_logger, ex);
            throw;
        }
    }
}
