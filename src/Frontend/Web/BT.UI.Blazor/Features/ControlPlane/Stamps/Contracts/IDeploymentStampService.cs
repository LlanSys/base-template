using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Stamps.Dtos;

namespace BT.UI.Blazor.Features.ControlPlane.Stamps.Contracts;

internal interface IDeploymentStampService
{
    Task<AppResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppResponse<bool>> SetAvailabilityAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
    Task<AppResponse<List<DeploymentStampResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AppResponse<DeploymentStampResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppResponse<DeploymentStampResponse>> CreateAsync(CreateDeploymentStampRequest request, CancellationToken cancellationToken = default);
    Task<AppResponse<DeploymentStampResponse>> UpdateAsync(Guid id, UpdateDeploymentStampRequest request, CancellationToken cancellationToken = default);
}
