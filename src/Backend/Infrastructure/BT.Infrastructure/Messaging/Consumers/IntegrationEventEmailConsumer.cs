using BT.SharedKernel.Features.Shared.Notifications.Dtos;
using BT.Application.Contracts.Interfaces.Common;
using BT.Application.Features.IAM.Users.Contracts.Interfaces;
using BT.Application.Features.Shared.Notifications.Contracts.Interfaces;
using BT.SharedKernel.Extensions;
using BT.Application.Features.Banking.Customers.IntegrationEvents;
using BT.Application.Features.HR.Employees.IntegrationEvents;
using BT.Application.Features.IAM.Users.IntegrationEvents;
using BT.Application.Features.Banking.Customers.Mappings;
using BT.Application.Features.HR.Employees.Mappings;
using BT.Application.Features.IAM.Users.Mappings;
using BT.Application.Features.Shared.EmailTemplates.Mappings;
using BT.Domain.Features.Banking.Contracts;
using BT.Domain.Features.Banking.Customers.Entities;
using BT.Domain.Features.Banking.Customers.Enums;
using BT.Domain.Features.HR.Contracts;
using BT.Domain.Features.IAM.Contracts;
using BT.Domain.Shared.Contracts;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.Entities;
using BT.Domain.Features.Shared.FailedMessages.Enums;

using BT.Infrastructure.Logging;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.EmailTemplates.Enums;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BT.Infrastructure.Messaging.Consumers;

public abstract class IntegrationEventEmailConsumer<TEvent>(
    TimeProvider timeProvider,
    IEmailComposer<TEvent> composer,
    ISharedUnitOfWork sharedUnitOfWork,
    ILogger<IntegrationEventEmailConsumer<TEvent>> logger) : IConsumer<TEvent> where TEvent : class, IIntegrationEvent

{
    public async Task Consume(ConsumeContext<TEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        var evt = context.Message;
        var retryCount = context.GetRetryCount();
        var messageId = context.MessageId?.ToString() ?? Guid.CreateVersion7().ToString();

        try
        {
            var composed = await composer.ComposeAsync(evt, context.CancellationToken).ConfigureAwait(false);

            if (!composed.IsSuccess || composed.Data is null)
            {
                MessageBusLogDefinitions.LogEmailCompositionFailed(logger, messageId);
                return;
            }

            await context.Publish(new SendEmailRequest
            {
                To = composed.Data.RecipientEmail,
                Subject = composed.Data.Subject,
                Body = composed.Data.Body

            }, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (retryCount >= 4)
        {
            MessageBusLogDefinitions.LogPermanentEventFailure(logger, typeof(TEvent).Name, retryCount + 1, ex);

            var failed = FailedMessage.RecordPermanentFailure(
                timeProvider.GetUtcNow(),
                messageId,
                typeof(TEvent).FullName!,
                JsonSerializer.Serialize(evt),
                ex.Message,
                retryCount + 1,
                GetType().Name,
                messageId,
                ex.StackTrace);

            await sharedUnitOfWork.FailedMessageRepository.CreateAsync(failed, context.CancellationToken).ConfigureAwait(false);
            await sharedUnitOfWork.CompleteAsync(context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            MessageBusLogDefinitions.LogTransientEventFailure(logger, typeof(TEvent).Name, retryCount + 1, ex);
            throw; // let MassTransit retry
        }
    }
}
