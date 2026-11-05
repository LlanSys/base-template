using System;
using System.Threading;
using System.Threading.Tasks;

using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Application.Features.ControlPlane.Tenants.Contracts;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.Domain.Shared.Contracts.Common;
using BT.Infrastructure.Logging;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using BT.SharedKernel.Extensions;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

public class ApproveTenantKYCCommandHandler : IRequestHandler<ApproveTenantKYCCommand, AppResponse<TenantResponse>>
{
    private readonly TimeProvider _timeProvider;
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly ILogger<ApproveTenantKYCCommandHandler> _logger;
    private readonly IStampProvisioner _stampProvisioner;
    private readonly ICurrentActorProvider _actorProvider;

    public ApproveTenantKYCCommandHandler(
        TimeProvider timeProvider,
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<ApproveTenantKYCCommandHandler> logger,
        IStampProvisioner stampProvisioner,
        ICurrentActorProvider actorProvider)
    {
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _stampProvisioner = stampProvisioner;
        _actorProvider = actorProvider;
    }

    public async Task<AppResponse<TenantResponse>> Handle(ApproveTenantKYCCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var tenant = await _unitOfWork.Tenants.FindByIdAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        if (tenant == null)
        {
            return AppResponses.Failure<TenantResponse>("Tenant not found.");
        }

        if (tenant.Status != TenantStatus.PendingKYC)
        {
            return AppResponses.Failure<TenantResponse>($"Cannot approve KYC from status {tenant.Status}.");
        }

        var stamp = await _unitOfWork.DeploymentStamps.FirstOrDefaultAsync(s => s.Id == tenant.DeploymentStampId, cancellationToken).ConfigureAwait(false);

        if (stamp == null)
        {
            return AppResponses.Failure<TenantResponse>("Deployment stamp for this tenant could not be found.");
        }

        tenant.ApproveKYC(_timeProvider.GetUtcNow(), _actorProvider.ActorId);

        if (stamp.IsolationTier == IsolationTier.Isolated)
        {
            tenant.MarkAsProvisioning();

            try
            {
                await _stampProvisioner.ProvisionIsolatedStampAsync(
                    tenant.Id.ToString(),
                    stamp.Name,
                    stamp.TargetResourceGroup,
                    tenant.DatabaseProvider ?? stamp.DatabaseProvider ?? "PostgreSql",
                    cancellationToken).ConfigureAwait(false);

                ControlPlaneLogDefinitions.LogStampProvisioningDispatched(_logger, tenant.Id, stamp.Name);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ControlPlaneLogDefinitions.LogStampProvisioningFailed(_logger, tenant.Id, ex);

                tenant.MarkAsProvisioningFailed();
                // Optionally log the failure, but we want to allow retry via UI or manual intervention.
            }
        }
        else
        {
            // If it's a shared stamp, no infra to provision, go straight to Active.
            tenant.MarkAsActive();
        }

        await _unitOfWork.Tenants.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        var dto = new TenantResponse
        {
            Id = tenant.Id,
            Revision = tenant.Revision,
            Identifier = tenant.Identifier,
            DisplayName = tenant.DisplayName,
            HostName = tenant.HostName,
            ContactEmail = tenant.ContactEmail,
            MaxUsers = tenant.MaxUsers,
            SubscriptionTier = tenant.SubscriptionTier.ToDisplayString(),
            Status = tenant.Status.ToDisplayString(),
            DeploymentStampId = tenant.DeploymentStampId,
            DatabaseProvider = tenant.DatabaseProvider,
            DatabaseConnectionString = tenant.DatabaseConnectionString != null ? "********" : null,
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };

        return AppResponses.Success(dto);
    }
}
