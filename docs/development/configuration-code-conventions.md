# Configuration Code Conventions

This document defines how application configuration values should be consumed in code.

For layout conventions and formatter commands, see [Source readability and formatting](code-readability.md).

## Type names and using directives

Use namespace imports so signatures and business logic contain short type names:

```csharp
using BT.Persistence.Features.IAM.DataContext;

internal sealed class ExampleHandler(IamDBContext context) { }
```

Do not write `Persistence.Features.IAM.DataContext.IamDBContext` or its fully qualified equivalent in the constructor. Apply the same rule in generic arguments, attributes, static calls, and Razor injection directives. Use feature-local or shared Razor `@using` directives as appropriate.

When names collide, use a descriptive alias, for example `using RefreshTokenEntity = BT.Domain.Features.IAM.Users.Entities.RefreshToken;`. Prefer file-local imports; use global imports only for genuinely shared, stable dependencies. Preserve fully qualified strings used for reflection, serialization, configuration, or namespace-based discovery. Generated migrations/designers are provider-owned historical artifacts and are excluded from style rewrites.

## TimeProvider and timestamp values

`TimeProvider` supplies the clock; `DateTimeOffset`, `DateTime`, and `DateOnly` represent data. Keep existing database columns and DTO timestamp types. Use UTC `DateTimeOffset` for instants; use `DateOnly` and an explicitly selected time zone for business calendar dates. Use `.UtcDateTime` only when a framework contract, such as JWT construction, requires `DateTime`.

New or changed runtime services receive `TimeProvider` through DI. Composition roots register `TimeProvider.System` once using `TryAddSingleton` where registrations can be composed. This preserves an explicit test replacement. Do not hide a system clock inside a service or use a mutable global clock.

```csharp
internal sealed class InvitationPolicy(TimeProvider timeProvider)
{
    public bool HasExpired(DateTimeOffset expiresAt)
    {
        var now = timeProvider.GetUtcNow();
        return expiresAt <= now;
    }
}
```

Capture one instant for a decision and pass it into database predicates or domain transitions. Domain entities should accept captured instants rather than reference a service container. For clock-driven scheduling, use `PeriodicTimer(interval, timeProvider)` and `Task.Delay(delay, timeProvider, cancellationToken)`. Preserve cancellation and use provider timestamps for elapsed-time measurement when needed. Do not change local/calendar semantics to UTC accidentally.

Tests supply a controllable provider and advance it across expiration boundaries without sleeping. Real identity-provider protocol checks and other framework clocks must be configured consistently with the application clock. Microsoft documents these APIs in the [TimeProvider overview](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview).

BaseTemplate applies this convention to IAM, onboarding, audit stamping, payments, timers and retry delays, calendar validators, and Blazor date defaults. Runtime contexts receive the registered clock. Context constructors retain a system default for direct design-time and tooling callers. SaveChanges captures one instant for the audit fields and administration audit entries it writes. Event publishers carry the captured domain instant into the existing `OccurredAt` field. Preserve deterministic EF seeds and historical migrations. Track StarterTemplate adoption in the [template parity checklist](../architecture/invitation-acceptance-and-starter-parity.md).

## Typed Mode And Provider Selection

Configuration values may remain strings in JSON, user-secrets, environment variables, and Key Vault because that is how .NET configuration works. Once those values enter code, parse mode/provider/transport values once into a typed enum and branch on the enum.

Do this:

```csharp
var mode = GetSomeMode(settings);

switch (mode)
{
    case SomeMode.Local:
        ConfigureLocal();
        break;

    case SomeMode.Azure:
        ConfigureAzure(settings);
        break;

    case SomeMode.Invalid:
    default:
        throw new InvalidOperationException(
            $"SomeSection:Mode '{settings.Mode}' is not supported. Supported values: Local, Azure.");
}
```

Avoid this:

```csharp
if (settings.Mode == "Azure" || settings.Mode == "Cloud")
{
    // ...
}
else
{
    // silently falls back to local
}
```

## Rules

- Each public settings POCO must live in its own file named after the type. Do not bundle nested settings classes in the parent settings file.
- Provider integrations must keep provider-specific request and response DTOs beside the provider adapter. Shared application contracts should expose canonical business inputs/results only, not Stripe, M-Pesa, Azure, or other provider wire payloads.
- Per-operation provider selection must go through a router or factory that delegates to isolated provider adapters. The configured provider is the default fallback, not a reason to hard-wire a single implementation at DI startup.
- Parse string configuration modes in one helper such as `GetKeyEncryptionMode` or `GetMessagingTransport`.
- Use a small enum for each bounded configuration choice.
- Keep each configuration branch in a one-purpose method.
- Fail fast on unsupported values with a message that names the section key and supported values.
- Do not silently fall back to a default when a non-empty value is invalid.
- Defaults are allowed only when the value is missing or blank and the default is documented.
- Keep auto-resolution explicit. For example, `Auto` can prefer Key Vault, then certificate, then no extra encryption, but this priority must live in a named method.

## Where Enums Belong

Not every enum belongs in the Domain layer.

Put enums in Domain only when they describe business language or invariants that belong to the model, such as customer status, gender, account state, role scope, approval state, or loan status.

Keep enums in the owning technical layer when they describe infrastructure, hosting, storage, transport, framework, or configuration choices. These are implementation details, even when the possible values are stable. Examples include Data Protection encryption mode, profile-image storage provider, auth provider, cache provider, and messaging transport.

Use this rule of thumb:

- If a business expert would use the term in a process discussion, it is probably Domain or SharedKernel.
- If only developers, DevOps, Azure, framework setup, or hosting configuration care about it, keep it in the owning layer.
- If the enum is only used to parse one settings POCO, keep it private near the parser.
- If the enum must cross API/UI boundaries, expose a DTO-friendly value and use the established enum conversion helpers at the boundary.

## Current Examples

- `DataProtection:KeyEncryptionMode` is parsed into `KeyEncryptionMode`.
- `Messaging:Transport` is parsed into `MessagingTransport`.
- `ProfileImageStorage:Provider` is parsed into `ProfileImageStorageProvider`.
- `AuthProvider:Provider` is parsed into `AuthProvider`.
- `EmailSettings:Provider` is parsed into `EmailProvider`.
- `Payments:Provider` is parsed into `PaymentProviderKind`, while `PaymentInitiationRequest.Provider` can override the default per payment.

When adding a new configurable provider, transport, mode, or strategy, follow the same pattern and update this document if the convention changes.

## Async resource lifetime

Configure awaiting the acquisition separately from awaiting disposal. Keep the original object for operations such as transaction commit, rollback, and service resolution:

```csharp
var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
await using var transactionLifetime = transaction.ConfigureAwait(false);
await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
```

Do not assign `resource.ConfigureAwait(false)` to the variable used to call resource methods: that returns a disposal wrapper, not the original resource. Apply the same rule to DI scopes, service providers, Redis connections, and provider-specific DbContexts. Conditional construction of provider-specific contexts must target their common base type before configuring disposal. This follows the [Microsoft async-disposal pattern](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync#using-async-disposable).

Assigning the wrapper to an intermediate variable and then using `await using var transactionLifetime = configuredAsyncDisposable;` is also valid. The intermediate variable is optional; the two-line example above is sufficient. Continue calling resource operations on `transaction`.
