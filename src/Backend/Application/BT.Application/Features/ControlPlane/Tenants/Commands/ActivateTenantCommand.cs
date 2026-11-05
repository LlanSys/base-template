using BT.Application.Contracts.Interfaces.Common;
using System.Collections.Generic;
using System;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using MediatR;
namespace BT.Application.Features.ControlPlane.Tenants.Commands;

public record ActivateTenantCommand(Guid Id) : IRequest<AppResponse<TenantResponse>>, ICacheInvalidatorRequest
{
    public IEnumerable<string> CacheGroups => ["tenants"];
    public string? CacheUserId => null;
}
