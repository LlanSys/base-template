using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Features.Banking.Customers.Contracts.Repositories;
using BT.Domain.Features.HR.Employees.Contracts.Repositories;
using BT.Domain.Features.IAM.Users.Contracts.Repositories;
using BT.Domain.Features.Shared.Payments.Contracts.Repositories;
using BT.Domain.Features.Shared.OrgSettings.Contracts.Repositories;
using BT.Domain.Shared.Contracts.Repositories;
using BT.Persistence.Common;
using BT.Persistence.Logging;
using BT.Persistence.Features.Shared.DataContext;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BT.Persistence.Features.Shared;

public sealed class SharedUnitOfWork(
    TimeProvider timeProvider,
    SharedDBContext context,
    IEmailTemplateRepository emailTemplateRepository,
    IFailedMessageRepository failedMessageRepository,
    IPaymentRecordRepository paymentRecordRepository,
    IOrgSettingRepository OrgSettingRepository,
    IPublisher publisher,
    ILogger<SharedUnitOfWork> logger
) : BaseUnitOfWork<SharedDBContext>(context, publisher, logger, timeProvider), ISharedUnitOfWork
{
    private readonly SharedDBContext _sharedContext = context;
    private readonly IPublisher _publisher = publisher;
    private readonly ILogger<SharedUnitOfWork> _logger = logger;

    public IEmailTemplateRepository EmailTemplateRepository { get; } = emailTemplateRepository;
    public IFailedMessageRepository FailedMessageRepository { get; } = failedMessageRepository;
    public IPaymentRecordRepository PaymentRecordRepository { get; } = paymentRecordRepository;
    public IOrgSettingRepository OrgSettingRepository { get; } = OrgSettingRepository;

    public async Task<int> CompleteWithEventsAsync(List<IIntegrationEvent>? appEvents = null, CancellationToken ct = default)
    {
        var transaction = await _sharedContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        await using var configuredTransaction = transaction.ConfigureAwait(false);

        try
        {
            var result = await _sharedContext.SaveChangesAsync(ct).ConfigureAwait(false);
            var domainEvents = _sharedContext.GetCollectedDomainEvents() ?? Array.Empty<IDomainEvent>();

            foreach (var domainEvent in domainEvents)
                await _publisher.Publish(domainEvent, ct).ConfigureAwait(false);

            if (appEvents != null)
                foreach (var appEvent in appEvents)
                    await _publisher.Publish(appEvent, ct).ConfigureAwait(false);

            await transaction.CommitAsync(ct).ConfigureAwait(false);
            _sharedContext.ClearCollectedDomainEvents();

            PersistenceLogDefinitions.LogEventsPublished(_logger, domainEvents.Count);
            return result;
        }
        catch (Exception ex)
        {
            PersistenceLogDefinitions.LogCompleteWithEventsRollback(_logger, ex);
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
            _sharedContext.ClearCollectedDomainEvents();
            throw;
        }
    }
}
