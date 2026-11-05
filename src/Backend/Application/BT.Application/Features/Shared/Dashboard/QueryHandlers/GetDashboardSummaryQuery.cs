using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Utilities;
using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.Banking.Customers.Enums;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.Dashboard.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using BT.SharedKernel.Extensions;

namespace BT.Application.Features.Shared.Dashboard.QueryHandlers;


public record GetDashboardSummaryQuery(string UserId, string? RoleScope = null) : IRequest<AppResponse<DashboardSummaryResponse>>, ICachableRequest

{
    public string CacheGroup => "dashboard";
    public string Discriminator => CacheKeys.Discriminator(new { RoleScope });
    public string? CacheUserId => null;
    public bool IsVersioned => true;
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}

