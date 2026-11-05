using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Stamps.Dtos;
using BT.UI.Blazor.Features.ControlPlane.Stamps.Contracts;
using BT.UI.Blazor.Features.Shared.BackendApi.Contracts.Interfaces;

namespace BT.UI.Blazor.Features.ControlPlane.Stamps.Implementations;

internal sealed class DeploymentStampService(IBackendApiClient client) : IDeploymentStampService
{
    private const string BasePath = "api/v1/control-plane/stamps";

    public async Task<AppResponse<List<DeploymentStampResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<List<DeploymentStampResponse>>(
            HttpMethod.Get,
            BasePath,
            requiresAuthentication: true,
            unavailableMessage: "Unable to retrieve deployment stamps.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<DeploymentStampResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<DeploymentStampResponse>(
            HttpMethod.Get,
            $"{BasePath}/{id}",
            requiresAuthentication: true,
            unavailableMessage: "Unable to retrieve deployment stamp.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<DeploymentStampResponse>> CreateAsync(CreateDeploymentStampRequest request, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<DeploymentStampResponse>(
            HttpMethod.Post,
            BasePath,
            request: request,
            requiresAuthentication: true,
            unavailableMessage: "Unable to create deployment stamp.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppResponse<DeploymentStampResponse>> UpdateAsync(Guid id, UpdateDeploymentStampRequest request, CancellationToken cancellationToken = default)
    {
        return await client.SendAsync<DeploymentStampResponse>(
            HttpMethod.Put,
            $"{BasePath}/{id}",
            request: request,
            requiresAuthentication: true,
            unavailableMessage: "Unable to update deployment stamp.",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public Task<AppResponse<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => client.SendAsync<bool>(HttpMethod.Delete, $"{BasePath}/{id}", requiresAuthentication: true, cancellationToken: cancellationToken);

    public Task<AppResponse<bool>> SetAvailabilityAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
        => client.SendAsync<bool>(HttpMethod.Put, $"{BasePath}/{id}/availability", request: isActive, requiresAuthentication: true, cancellationToken: cancellationToken);
}
