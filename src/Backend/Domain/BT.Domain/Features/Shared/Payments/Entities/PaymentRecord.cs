using BT.Domain.Features.Shared.Payments.Enums;
using BT.Domain.Features.Shared.Payments.Events;
using BT.Domain.Shared.Contracts.Common;
using BT.Domain.Shared.Entities;
using BT.Domain.Shared.ValueObjects;

namespace BT.Domain.Features.Shared.Payments.Entities;

public class PaymentRecord : BaseEntity, IAuditable, ISoftDeletable, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();
    public void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public Money Amount { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string CustomerReference { get; private set; } = null!;
    public string Provider { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public string? ProviderReference { get; private set; }
    public string? StatusMessage { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string? CheckoutUrl { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public void MarkAsDeleted(string deletedBy, DateTimeOffset now)
    {
        IsDeleted = true;
        DeletedAt = now;
        DeletedBy = deletedBy;
    }

    // Required by EF Core
    protected PaymentRecord() { }

    public PaymentRecord(
        Guid id,
        Money amount,
        string description,
        string customerReference,
        string provider,
        string? idempotencyKey = null,
        PaymentStatus status = PaymentStatus.Initiated)
    {
        Id = id;
        Amount = amount;
        Description = description;
        CustomerReference = customerReference;
        Provider = provider;
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        Status = status;
    }

    public void UpdateStatus(DateTimeOffset now, PaymentStatus status, string? providerReference = null, string? statusMessage = null)
    {
        if (Status == PaymentStatus.Success || Status == PaymentStatus.Failed || Status == PaymentStatus.Cancelled)
        {
            // Already in a terminal state, ignore updates to ensure strict idempotency
            return;
        }

        Status = status;

        if (providerReference != null)
        {
            ProviderReference = providerReference;
        }

        if (statusMessage != null)
        {
            StatusMessage = string.IsNullOrWhiteSpace(statusMessage) ? null : statusMessage.Trim();
        }

        if (status == PaymentStatus.Success)
        {
            RaiseDomainEvent(new PaymentCompletedEvent(Id, CustomerReference, Provider, Amount) { OccurredAt = now });
        }
        else if (status == PaymentStatus.Failed)
        {
            RaiseDomainEvent(new PaymentFailedEvent(Id, CustomerReference, Provider, Amount, StatusMessage ?? "Asynchronous payment failed.") { OccurredAt = now });
        }
        else if (status == PaymentStatus.Cancelled)
        {
            RaiseDomainEvent(new PaymentCancelledEvent(Id, CustomerReference, Provider, Amount, StatusMessage ?? "Asynchronous payment was cancelled.") { OccurredAt = now });
        }
    }

    public void SetCheckoutUrl(string? checkoutUrl)
    {
        CheckoutUrl = string.IsNullOrWhiteSpace(checkoutUrl) ? null : checkoutUrl.Trim();
    }

    public void SetProviderReference(string? providerReference)
    {
        ProviderReference = string.IsNullOrWhiteSpace(providerReference) ? null : providerReference.Trim();
    }
}
