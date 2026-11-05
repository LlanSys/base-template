using BT.Application.Contracts.Interfaces.Common;
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

using System;
using System.Collections.Generic;
using System.Text;

namespace BT.Application.Features.Banking.Customers.CommandHandlers;


/// <summary>
/// Invalidation:
///   - Direct:  delete the entity entry so the next GetById call fetches fresh data.
///   - Version: bump the global "customers" and "dashboard" versions to orphan list entries.
///
/// Both are necessary: without the direct deletion the entity detail page would
/// still show stale data even after the list refreshes.
/// </summary>

public record UpdateCustomerCommand(Guid Id, UpdateCustomerRequest UpdateCustomerRequest, string UserId)
    : IRequest<AppResponse<CustomerResponse>>, ICacheInvalidatorRequest
{
    public IReadOnlyList<string> DirectInvalidationKeys => [CacheKeys.Entity("customers", Id.ToString())];
    public IReadOnlyList<string> GroupVersionKeysToInvalidate =>
    [
        CacheKeys.GroupVersion("customers"),
        CacheKeys.GroupVersion("dashboard")
    ];

}

