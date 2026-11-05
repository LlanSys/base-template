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

public record GetCustomerListQuery(CustomerListRequest CustomerListRequest, string UserId)
    : IRequest<AppResponse<PagedResponse<CustomerResponse, Guid>>>, ICachableRequest
{
    public string CacheGroup => "customers";
    public string Discriminator => CacheKeys.Discriminator(new CustomerListRequest(CustomerListRequest.Cursor, CustomerListRequest.PageSize));
    public string? CacheUserId => null;
    public bool IsVersioned => true;
}

