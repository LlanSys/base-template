using BT.SharedKernel.Dtos.Common;
using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Utilities;
using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.Banking.Customers.Entities;
using BT.SharedKernel.Features.Banking.Customers.Dtos;

using MediatR;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Text;

namespace BT.Application.Features.Banking.Customers.CommandHandlers;

/// <summary>
/// Same invalidation pattern as Update: remove the entity entry + bump the list version.
/// </summary>

public record DeleteCustomerCommand(Guid CustomerId, string UserId) : IRequest<AppResponse<bool>>, ICacheInvalidatorRequest

{
    public IReadOnlyList<string> DirectInvalidationKeys => [CacheKeys.Entity("customers", CustomerId.ToString())];
    public IReadOnlyList<string> GroupVersionKeysToInvalidate =>
    [
        CacheKeys.GroupVersion("customers"),
        CacheKeys.GroupVersion("dashboard")
    ];

}

