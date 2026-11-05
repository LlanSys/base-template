using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.ControlPlane.Tenants.Commands;
using BT.Application.Features.ControlPlane.Tenants.Contracts;
using BT.Domain.Features.ControlPlane.Contracts;
using BT.Domain.Features.ControlPlane.Tenants.Entities;
using BT.Domain.Features.ControlPlane.Tenants.Enums;
using BT.Domain.Shared.Contracts.Common;
using BT.Infrastructure.Features.ControlPlane.Tenants.CommandHandlers;
using BT.SharedKernel.Extensions;
using BT.SharedKernel.Features.ControlPlane.Tenants.Dtos;

using Microsoft.Extensions.Logging;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Xunit;

namespace BT.Tests.Unit.ControlPlane.Tenants;

public sealed class CreateTenantCommandHandlerTests
{
    private readonly IControlPlaneUnitOfWork _unitOfWork;
    private readonly IStampProvisioner _provisioner;
    private readonly ILogger<CreateTenantCommandHandler> _logger;
    private readonly IEncryptionService _encryptionService;
    private readonly ICurrentActorProvider _actorProvider;
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests()
    {
        _unitOfWork = Substitute.For<IControlPlaneUnitOfWork>();
        _provisioner = Substitute.For<IStampProvisioner>();
        _logger = Substitute.For<ILogger<CreateTenantCommandHandler>>();
        _encryptionService = Substitute.For<IEncryptionService>();
        _actorProvider = Substitute.For<ICurrentActorProvider>();
        _actorProvider.ActorId.Returns("test-actor-id");
        _handler = new CreateTenantCommandHandler(TimeProvider.System, _unitOfWork, _logger, _encryptionService, _provisioner, _actorProvider);
    }

    [Fact]
    public async Task Handle_WhenTenantAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var request = new CreateTenantRequest
        {
            InitialAdministrator = new("admin@example.test", "Initial", "Administrator"),
            DisplayName = "Test",
            Identifier = "test-tenant",
            DeploymentStampId = Guid.NewGuid(),
            SubscriptionTier = "Enterprise",
            HostName = "test.example.com"
        };

        var command = new CreateTenantCommand(request);

        _unitOfWork.Tenants.AnyAsync(
            Arg.Any<Expression<Func<Tenant, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.Message, StringComparison.OrdinalIgnoreCase);
        await _unitOfWork.Tenants.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().CompleteAsync(default);
    }

    [Fact]
    public async Task Handle_WhenStampDoesNotExist_ReturnsFailure()
    {
        // Arrange
        var request = new CreateTenantRequest
        {
            InitialAdministrator = new("admin@example.test", "Initial", "Administrator"),
            DisplayName = "Test",
            Identifier = "test-tenant",
            DeploymentStampId = Guid.NewGuid(),
            SubscriptionTier = "Enterprise",
            HostName = "test.example.com"
        };

        var command = new CreateTenantCommand(request);

        _unitOfWork.Tenants.AnyAsync(
            Arg.Any<Expression<Func<Tenant, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork.DeploymentStamps.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<DeploymentStamp, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns((DeploymentStamp)null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("does not exist", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WhenSubscriptionTierIsInvalid_ReturnsFailure()
    {
        // Arrange
        var request = new CreateTenantRequest
        {
            InitialAdministrator = new("admin@example.test", "Initial", "Administrator"),
            DisplayName = "Test",
            Identifier = "test-tenant",
            DeploymentStampId = Guid.NewGuid(),
            SubscriptionTier = "InvalidTier123",
            HostName = "test.example.com"
        };

        var command = new CreateTenantCommand(request);

        _unitOfWork.Tenants.AnyAsync(
            Arg.Any<Expression<Func<Tenant, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork.DeploymentStamps.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<DeploymentStamp, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(new DeploymentStamp { Id = request.DeploymentStampId, Name = "Stamp", TargetResourceGroup = "test-rg", CreatedBy = "test" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid Subscription Tier", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithValidRequest_CreatesTenantWithPendingKYCStatus()
    {
        // Arrange
        var stampId = Guid.NewGuid();
        var request = new CreateTenantRequest
        {
            InitialAdministrator = new("admin@example.test", "Initial", "Administrator"),
            DisplayName = "Test Tenant",
            Identifier = "test-tenant",
            DeploymentStampId = stampId,
            SubscriptionTier = "Enterprise",
            ContactEmail = "test@example.com",
            DatabaseProvider = "SqlServer",
            HostName = "test.example.com",
            DatabaseConnectionString = "Server=localhost;Database=db;",
            ModuleKeys = ["Banking"]
        };

        var command = new CreateTenantCommand(request);

        var stamp = new DeploymentStamp
        {
            Id = stampId,
            Name = "Stamp",
            IsolationTier = IsolationTier.Pooled,
            TargetResourceGroup = "test-rg",
            CreatedBy = "test-user"
        };

        _unitOfWork.DeploymentStamps.FirstOrDefaultAsync(
            Arg.Any<Expression<Func<DeploymentStamp, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(stamp);

        _unitOfWork.Tenants.AnyAsync(
            Arg.Any<Expression<Func<Tenant, bool>>>(),
            Arg.Any<CancellationToken>())
            .Returns(false);

        _unitOfWork.Modules.ListAsync(
            Arg.Any<Func<IQueryable<Domain.Features.ControlPlane.Modules.Entities.ModuleDefinition>, IQueryable<Domain.Features.ControlPlane.Modules.Entities.ModuleDefinition>>>(),
            Arg.Any<CancellationToken>()).Returns(new List<Domain.Features.ControlPlane.Modules.Entities.ModuleDefinition>
            {
                new() { Key = "Banking", Name = "Banking", IsActive = true, CreatedBy = "test-user" }
            });

        _encryptionService.Encrypt(Arg.Any<string>()).Returns("encrypted-string");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TenantStatus.PendingKYC.ToDisplayString(), result.Data!.Status);

        await _unitOfWork.Tenants.Received(1).CreateAsync(
            Arg.Is<Tenant>(t =>
                t.Status == TenantStatus.PendingKYC &&
                t.Identifier == "test-tenant" &&
                t.DatabaseConnectionString == "encrypted-string" && t.Modules.Any(module => module.ModuleKey == "Banking")),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());

        // Ensure provisioner is NOT called directly from this handler anymore
        await _provisioner.DidNotReceiveWithAnyArgs().ProvisionIsolatedStampAsync(
            default!, default!, default!, default!, default);
    }
}
