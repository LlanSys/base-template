using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.Banking.Customers.Contracts.Interfaces;
using BT.Application.Features.Banking.Customers.IntegrationEvents;
using BT.Application.Features.HR.Employees.IntegrationEvents;
using BT.Application.Features.IAM.Users.IntegrationEvents;
using BT.SharedKernel.Extensions;
using BT.Application.Features.Banking.Customers.Mappings;
using BT.Application.Features.HR.Employees.Mappings;
using BT.Application.Features.IAM.Users.Mappings;
using BT.Application.Features.Shared.EmailTemplates.Mappings;
using BT.Application.Utilities;
using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.Banking.Customers.Entities;
using BT.Domain.Features.Banking.Customers.Enums;
using BT.Domain.Features.Banking.Customers.ValueObjects;
using BT.SharedKernel.Features.Banking.Customers.Dtos;
using BT.SharedKernel.Dtos.Common;

using MediatR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BT.Application.Features.Banking.Customers.CommandHandlers;

/// <summary>
/// Invalidation: bump the global version tokens for customer lists and dashboards.
/// No entity key to delete (the entity does not exist in cache yet).
/// Versioned list entries are orphaned in O(1).
/// </summary>

public sealed record CreateCustomerCommand(CreateCustomerRequest CreateCustomerRequest, string UserId)
    : IRequest<AppResponse<CustomerResponse>>, ICacheInvalidatorRequest
{
    // No direct keys — new entity, nothing cached yet.
    public IReadOnlyList<string> DirectInvalidationKeys => [];

    public IReadOnlyList<string> GroupVersionKeysToInvalidate =>
    [
        CacheKeys.GroupVersion("customers"),
        CacheKeys.GroupVersion("dashboard")
    ];


}

