using System.Collections.Generic;
using BT.Application.Contracts.Interfaces.Common;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using MediatR;

namespace BT.Application.Features.ControlPlane.Stamps.Queries;

public record GetAllDeploymentStampsQuery : IRequest<AppResponse<List<DeploymentStampResponse>>>, ICachableRequest
{
    public string CacheGroup => "stamps";
    public string Discriminator => "all";
    public string? CacheUserId => null;
    public bool IsVersioned => true;

    public bool BypassCache => true;
}
