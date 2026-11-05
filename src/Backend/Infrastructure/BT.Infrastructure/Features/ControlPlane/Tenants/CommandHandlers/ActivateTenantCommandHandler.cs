using BT.Domain.Features.ControlPlane.Tenants.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.SharedKernel.Dtos.Common;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using BT.SharedKernel.Extensions;
using BT.Infrastructure.Logging;
using BT.Domain.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

public class ActivateTenantCommandHandler : IRequestHandler<ActivateTenantCommand, AppResponse<TenantResponse>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly ILogger<ActivateTenantCommandHandler> _logger;
    private readonly ICurrentActorProvider _actorProvider;

    private readonly TimeProvider _timeProvider;

    public ActivateTenantCommandHandler(
        TimeProvider timeProvider,
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<ActivateTenantCommandHandler> logger,
        ICurrentActorProvider actorProvider)
    {
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _actorProvider = actorProvider;
    }

    public async Task<AppResponse<TenantResponse>> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var tenant = await _unitOfWork.Tenants.FindByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);

        if (tenant == null)
        {
            return AppResponses.Failure<TenantResponse>("Tenant not found.");
        }

        if (tenant.Status is not (TenantStatus.Active or TenantStatus.Suspended or TenantStatus.Provisioning))
            return AppResponses.Failure<TenantResponse>("Complete onboarding and provisioning before activating this tenant.");
        tenant.Status = TenantStatus.Active;
        tenant.UpdatedAt = _timeProvider.GetUtcNow();
        tenant.UpdatedBy = _actorProvider.ActorId;

        await _unitOfWork.Tenants.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        ControlPlaneLogDefinitions.LogTenantActivated(_logger, tenant.Id, tenant.Identifier);

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
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };

        return AppResponses.Success(dto);
    }
}


