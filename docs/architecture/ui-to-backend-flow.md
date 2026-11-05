# UI-to-Backend Flow

The default product-building flow is:

1. Start from the UI journey.
2. Call backend APIs through typed HTTP clients.
3. Keep DTO contracts in `BT.SharedKernel`.
4. Keep UI state, presentation models, and browser/session concerns in the UI layer.
5. Push business rules into Application and Domain when the journey exposes them.

## Default Rule

Blazor Web is the only shipped frontend. Presentation service interfaces live beside their owning features in `BT.UI.Blazor`; transport DTOs remain in `BT.SharedKernel`. The web host references SharedKernel directly and has no separate component-library dependency.

User-facing confirmation text describes the action and its consequence, for example “Delete Acme? Users will lose access to this organization.” Persistence details such as soft deletion, retained rows, transaction handling, or background delivery leases belong in technical documentation unless the user must make a decision about them.

BaseTemplate's web UI uses Interactive Server rendering with protected component prerendering disabled. The initial HTTP response is an anonymous, empty application shell. The router restores the circuit's protected browser tokens after JavaScript becomes available, then renders native `AuthorizeRouteView` page policies and policy-protected layouts. All data and commands remain permission-protected at the separate API boundary. Never prerender protected content into that shell or copy bearer/refresh tokens into a shared cache. This arrangement also allows direct URLs to restore a session or display access denied instead of failing an unavailable HTTP authentication challenge.

The typed API client preserves the browser's organization hostname while connecting to the configured backend address, so anonymous sign-in and recovery resolve the same tenant as an invitation link. The hostname selects the catalogue tenant; it does not grant permissions or replace current-user and tenant validation at the API.

Administrator onboarding uses `/iam/invitations/accept`, then authenticator setup and final completion. The worker's email links to this dedicated journey, rather than password recovery. Keep acceptance credentials out of API query strings and disable caching/referrers on the welcome/callback pages. Ordinary API calls use `BackendApi:BaseUrl`; browser OIDC navigation uses `BackendApi:PublicBaseUrl`. The central callback validates protected tenant state before selecting the IAM scope. See [provider setup](../development/google-and-microsoft-sso.md).

Frontend projects must not reference backend Application, Domain, Persistence, EF Core contexts, repositories, or MediatR handlers directly.

Use this boundary:

```text
Blazor UI -> typed HTTP client -> BT.Api -> Application -> Domain -> Persistence/Infrastructure
```

This keeps the API contract real from day one. The same backend endpoints can later serve Blazor, mobile, external API clients, API Management, integration tests, and deployment probes.

## Why Not Call Application Directly?

Direct Application calls can reduce HTTP serialization overhead, but they also couple the UI deployment to backend internals. That coupling makes these things harder:

- Independent API/UI deployment.
- Azure API Management adoption.
- Mobile and external client reuse.
- Authentication and authorization consistency.
- Contract testing.
- Observability at the API boundary.
- Versioning and backward compatibility.

For this base template, the HTTP boundary is the enterprise default.

## Allowed Exception

Direct in-process Application calls are allowed only when a module is explicitly designed as one deployable unit and does not need to expose the same capability to API, mobile, external clients, or API Management.

Document the exception before using it.
