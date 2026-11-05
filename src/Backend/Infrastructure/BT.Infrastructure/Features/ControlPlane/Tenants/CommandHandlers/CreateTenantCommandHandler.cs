using BT.Application.Contracts.Interfaces.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.SharedKernel.Dtos.Common;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;
using BT.SharedKernel.Extensions;
using BT.Infrastructure.Logging;
using BT.Domain.Shared.Contracts.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.Application.Features.ControlPlane.Tenants.Contracts;

namespace BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, AppResponse<TenantResponse>>
{
    private readonly TimeProvider _timeProvider;
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTenantCommandHandler> _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly IStampProvisioner _stampProvisioner;
    private readonly ICurrentActorProvider _actorProvider;

    public CreateTenantCommandHandler(
        TimeProvider timeProvider,
        IControlPlaneUnitOfWork unitOfWork,
        ILogger<CreateTenantCommandHandler> logger,
        IEncryptionService encryptionService,
        IStampProvisioner stampProvisioner,
        ICurrentActorProvider actorProvider)
    {
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _encryptionService = encryptionService;
        _stampProvisioner = stampProvisioner;
        _actorProvider = actorProvider;
    }

    public async Task<AppResponse<TenantResponse>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentNullException.ThrowIfNull(request.Request, nameof(request.Request));

        var req = request.Request;
        var existingTenant = await _unitOfWork.Tenants.AnyAsync(t => t.Identifier == req.Identifier || t.HostName == req.HostName, cancellationToken).ConfigureAwait(false);

        if (existingTenant)
        {
            return AppResponses.Failure<TenantResponse>("A tenant with this identifier or hostname already exists.");
        }

        var stamp = await _unitOfWork.DeploymentStamps.FirstOrDefaultAsync(s => s.Id == req.DeploymentStampId, cancellationToken).ConfigureAwait(false);

        if (stamp == null || !stamp.IsActive)
        {
            return AppResponses.Failure<TenantResponse>("The specified Deployment Stamp does not exist.");
        }

        SubscriptionTier parsedTier;

        try
        {
            parsedTier = req.SubscriptionTier.ToEnum<SubscriptionTier>();
        }
        catch (ArgumentException)
        {
            return AppResponses.Failure<TenantResponse>("Invalid Subscription Tier.");
        }

        var moduleKeys = req.ModuleKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var availableModules = await _unitOfWork.Modules.ListAsync(
            modules => modules.Where(module => module.IsActive), cancellationToken).ConfigureAwait(false);

        if (moduleKeys.Length == 0 || moduleKeys.Any(key => !BT.SharedKernel.Features.ControlPlane.Modules.ModuleCatalog.IsImplemented(key)
            || !availableModules.Any(module => string.Equals(module.Key, key, StringComparison.OrdinalIgnoreCase))))
        {
            return AppResponses.Failure<TenantResponse>("Select at least one available business module.");
        }

        if (req.InitialAdministrator is null) return AppResponses.Failure<TenantResponse>("An initial administrator is required.");

        var tenant = new Tenant
        {
            Identifier = req.Identifier,
            DisplayName = req.DisplayName,
            HostName = req.HostName,
            ContactEmail = req.ContactEmail,
            MaxUsers = req.MaxUsers,
            SubscriptionTier = parsedTier,
            DeploymentStampId = req.DeploymentStampId,
            DatabaseProvider = req.DatabaseProvider,
            DatabaseConnectionString = !string.IsNullOrWhiteSpace(req.DatabaseConnectionString)
                ? _encryptionService.Encrypt(req.DatabaseConnectionString)
                : null,
            CreatedBy = _actorProvider.ActorId,
        };

        foreach (var key in moduleKeys)
            tenant.Modules.Add(new TenantModule { TenantId = tenant.Id, ModuleKey = key, CreatedBy = _actorProvider.ActorId });

        var administrator = req.InitialAdministrator;
        tenant.AdministratorInvitations.Add(new TenantAdministratorInvitation(_timeProvider.GetUtcNow())
        {
            TenantId = tenant.Id,
            Email = administrator.Email.Trim(),
            NormalizedEmail = administrator.Email.Trim().ToUpperInvariant(),
            FirstName = administrator.FirstName.Trim(),
            LastName = administrator.LastName.Trim(),
            CreatedBy = _actorProvider.ActorId
        });

        await _unitOfWork.Tenants.CreateAsync(tenant, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.CompleteAsync(cancellationToken).ConfigureAwait(false);

        ControlPlaneLogDefinitions.LogTenantCreated(_logger, tenant.Id, tenant.Identifier);

        var dto = new TenantResponse
        {
            AdministratorOnboarding = "Administrator invitation queued for activation",
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
            ,
            EnabledModules = moduleKeys.ToList()
        };

        return AppResponses.Success("Tenant and modules saved. Administrator invitation is queued and will be delivered after activation.", dto);
    }
}


