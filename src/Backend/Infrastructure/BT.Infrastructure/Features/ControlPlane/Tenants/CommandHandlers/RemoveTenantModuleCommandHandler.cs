using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Domain.Features.ControlPlane.Tenants.Events;
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

public class RemoveTenantModuleCommandHandler : IRequestHandler<RemoveTenantModuleCommand, AppResponse<TenantResponse>>
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly ILogger<RemoveTenantModuleCommandHandler> _logger;
    private readonly ICurrentActorProvider _actorProvider;

    private readonly TimeProvider _timeProvider;

    public RemoveTenantModuleCommandHandler(
        TimeProvider timeProvider,
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<RemoveTenantModuleCommandHandler> logger,
        ICurrentActorProvider actorProvider)
    {
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _actorProvider = actorProvider;
    }

    public async Task<AppResponse<TenantResponse>> Handle(RemoveTenantModuleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var tenant = await _unitOfWork.Tenants.FindAll()
            .Include(t => t.Modules)
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        if (tenant == null)
        {
            return AppResponses.Failure<TenantResponse>("Tenant not found.");
        }

        var moduleKey = request.Request.ModuleKey.Trim();
        var existingModule = tenant.Modules.FirstOrDefault(m => string.Equals(m.ModuleKey, moduleKey, StringComparison.OrdinalIgnoreCase));

        if (existingModule != null && existingModule.IsActive)
        {
            var now = _timeProvider.GetUtcNow();
            existingModule.IsActive = false;
            existingModule.UpdatedAt = now;
            existingModule.UpdatedBy = _actorProvider.ActorId;

            tenant.RaiseDomainEvent(new TenantModuleRevokedDomainEvent(tenant.Id, moduleKey) { OccurredAt = now });

            await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);
            ControlPlaneLogDefinitions.LogTenantModuleRemoved(_logger, moduleKey, tenant.Id);
        }

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
            EnabledModules = tenant.Modules.Where(m => m.IsActive).Select(m => m.ModuleKey).ToList(),
            CreatedAt = tenant.CreatedAt,
            UpdatedAt = tenant.UpdatedAt
        };

        return AppResponses.Success(dto);
    }
}
