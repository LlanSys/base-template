using System;
using BT.Application.Contracts.Interfaces.Common;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using MediatR;

namespace BT.Application.Features.ControlPlane.Tenants.Queries;

public record GetTenantByIdQuery(Guid Id) : IRequest<AppResponse<TenantResponse>>, ICachableRequest
{
    public string CacheGroup => "tenants";
    public string Discriminator => Id.ToString();
    public string? CacheUserId => null;
    public bool IsVersioned => false;

    public bool BypassCache => true;
}
