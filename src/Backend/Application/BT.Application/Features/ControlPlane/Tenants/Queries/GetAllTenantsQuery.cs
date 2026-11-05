using System;
using System.Collections.Generic;
using BT.Application.Contracts.Interfaces.Common;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using MediatR;

namespace BT.Application.Features.ControlPlane.Tenants.Queries;

public record GetAllTenantsQuery : IRequest<AppResponse<List<TenantResponse>>>, ICachableRequest
{
    public string CacheGroup => "tenants";
    public string Discriminator => "all";
    public string? CacheUserId => null;
    public bool IsVersioned => true;

    public bool BypassCache => true;
}
