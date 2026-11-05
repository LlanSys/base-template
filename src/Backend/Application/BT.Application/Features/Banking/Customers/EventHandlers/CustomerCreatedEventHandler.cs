using BT.Application.Contracts.Interfaces.Common;
using BT.SharedKernel.Extensions;
using BT.Application.Features.Banking.Customers.IntegrationEvents;
using BT.Application.Features.HR.Employees.IntegrationEvents;
using BT.Application.Features.IAM.Users.IntegrationEvents;
using BT.Application.Utilities;
using BT.Domain.Features.Banking.Customers.Events;
using BT.SharedKernel.Dtos.Common;
using BT.SharedKernel.Features.Shared.EmailTemplates.Enums;

using MediatR;

using Microsoft.Extensions.Logging;

namespace BT.Application.Features.Banking.Customers.EventHandlers;

public class CustomerCreatedEventHandler(
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<CustomerCreatedEventHandler> _logger)
    : INotificationHandler<CustomerCreatedEvent>
{
    public async Task Handle(CustomerCreatedEvent evt, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(evt, nameof(evt));

            var integrationEvent = new CustomerCreatedIntegrationEvent(
                evt.CustomerId,
                evt.Number,
                evt.Name,
                evt.Email,
                evt.Type.ToDisplayString())
            { OccurredAt = evt.OccurredAt };

            await integrationEventPublisher.PublishAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

            LogDefinitions.LogCustomerCreatedIntegrationPublished(_logger, evt.CustomerId);
        }
        catch (Exception ex)
        {
            LogDefinitions.LogCustomerCreatedDomainEventHandlerError(_logger, evt?.Email ?? string.Empty, ex);
            throw;
        }
    }
}
