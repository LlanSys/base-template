using System;
using BT.Application.Contracts.Interfaces.Common;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using MediatR;

namespace BT.Application.Features.ControlPlane.Stamps.Queries;

public record GetDeploymentStampByIdQuery(Guid Id) : IRequest<AppResponse<DeploymentStampResponse>>, ICachableRequest
{
    public string CacheGroup => "stamps";
    public string Discriminator => Id.ToString();
    public string? CacheUserId => null;
    public bool IsVersioned => false;

    public bool BypassCache => true;
}
