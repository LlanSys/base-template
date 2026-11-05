# Administrator invitation acceptance and template parity

Decision: 2026-11-05. Finish BaseTemplate first. StarterTemplate remains the neutral default for downstream applications; do not implement both repositories simultaneously.

KYC is a required platform capability in **both** templates. Tenant approval and provisioning must complete before administrator invitations can be delivered or accepted. Google joins Microsoft as a supported external sign-in provider. External sign-in does not implicitly waive administrator MFA.

## BaseTemplate acceptance contract

- Keep durable invitation delivery, leases, retry, audit, and tenant isolation.
- Deliver a dedicated welcome link; never route initial onboarding through Forgot Password or send passwords.
- Use random, expiring, purpose-specific invitation credentials, storing only hashes. Viewing a welcome page has no activation side effect. Require explicit acceptance.
- Distinguish email delivery from acceptance, support resend/revocation, and enforce one-time claiming with concurrency checks.
- Create an identity only on validated acceptance. Existing identities authenticate rather than resetting credentials through an invitation. Keep incomplete setup restricted; grant administrator access only after required MFA and final eligibility checks.
- Bind Microsoft/Google sign-in to validated provider identities and the intended organization. Prevent email-only account takeover, cross-tenant linking, untrusted redirects, and token storage in Redis.
- Existing local accounts connect a provider from Account security after confirming their current password; a two-minute, one-use database ticket binds the external login to that account. This connection is audited without recording provider subject values. Existing SSO-only accounts need an established local password before using this particular connection UI.
- Use provider-specific configuration and show only enabled providers. Live provider verification depends on configured OAuth applications and their registered callbacks.

## StarterTemplate implementation backlog

These items are required follow-up, not completed by BaseTemplate changes:

- [ ] Apply the shared using-directive convention: short type names in handwritten C#/Razor, descriptive aliases for actual collisions, and unchanged generated/reflection contracts.
- [ ] Apply the TimeProvider convention: injected clocks for runtime decisions and timers, deterministic time-boundary tests, and captured instants for domain operations. Preserve timestamp/DTO types and migration/seed values. Review the clock reads in StarterTemplate against its own architecture.

- [ ] Preserve/complete mandatory KYC review, approval permissions, audit evidence, provisioning, and activation gates.
- [ ] Port the tested invitation lifecycle, contracts, explicit welcome/acceptance UI, resend/revoke actions, and interrupted-setup recovery.
- [ ] Port Microsoft and Google provider configuration, invitation binding, safe redirects, identity linking, and MFA enforcement.
- [ ] Carry signed current-session MFA evidence through successful authenticator verification and token refresh; reject client-supplied proof and enrollment-only completion.
- [ ] Preserve the distinction between an internal API address and the central browser-facing OAuth callback address; protect tenant selection in OIDC state.
- [ ] Generate StarterTemplate-owned migrations for each supported provider; never copy BaseTemplate migration IDs or database state.
- [ ] Reuse delivery retries and audit patterns while preserving StarterTemplate's independently revised IAM and tenant model.
- [ ] Port negative tests for expired/revoked/replayed invitations, unapproved/suspended/deleted tenants, cross-tenant identities, concurrent acceptance, and MFA restrictions.
- [ ] Verify neutral downstream wording and email templates without Banking or HR dependencies.
- [ ] Run StarterTemplate's own builds, architecture checks, database upgrade/fresh checks, and browser/provider certification.

MassTransit remains pinned to 8.x for now. A later Wolverine migration or licensed MassTransit upgrade is a separate maintenance decision.

## BaseTemplate TimeProvider adoption

BaseTemplate routes runtime clock reads through `TimeProvider` across authentication, sessions, SSO, invitation lifecycle, entitlement expiry, domain transitions, audit stamping, payment flows, validation, and UI defaults. Domain transitions receive a timestamp value and event publishers preserve it in the existing message field. Apply the same boundaries using the domain and persistence design in StarterTemplate.

## Implementation and verification notes

- Added separate `AdministratorInvitationAcceptance` migrations for Control Plane and IAM under each database provider's feature-owned migration directory. The migrations add acceptance metadata, nullable account onboarding fields, and hashed SSO authorization codes. They do not reset passwords, alter existing grants, or assume old delivered invitations were accepted. Operators may resend an older invitation to issue the dedicated acceptance link.
- Issued invitation tokens last 48 hours by default (`AdministratorInvitations:ValidityHours`, range 1–72). Resend invalidates an old token; a retry sends the latest issued credential. Viewing the link does not consume it. Claiming is protected by the existing invitation revision. A claimed identity can sign in to resume MFA; an expired unused invitation requires resend. Revocation blocks completion. Accepted administrators are managed through their account, rather than invitation revocation.
- IAM and Control Plane can occupy separate databases. Completion deliberately uses idempotent stages: create a restricted identity, claim the invitation, verify MFA, assign the default role, persist acceptance, then clear restricted setup. Effective access remains empty while setup is pending, and completed onboarding must reference the accepted invitation. No distributed transaction is assumed.
- Automated checks: 106 unit tests; 84 architecture tests; 14 focused integration cases, comprising ten HTTP authorization/tenant/MFA cases, two expanded SQL Server/PostgreSQL upgrade cases, and two fresh-database cases. The upgrade cases cover queued-message preservation, invitation delivery/retry, password acceptance, external JIT acceptance, one-time SSO exchange, account connection, MFA gating, stale-revision rejection, and persisted revoke/reissue/resend through the real repositories after detaching entities.
- MFA completion requires both enrollment and signed application verification evidence from the current session. Only successful authenticator verification adds that evidence; refresh preserves existing evidence without deriving it from enrollment or external-provider claims. The HTTP regression test verifies that a client cannot assert MFA proof through a request body or query parameter.
- The additive migrations were applied to the existing local BaseTemplate SQL database only. All five contexts report zero pending migrations. Existing `cg-basetemplate` services were rebuilt/recreated; API readiness returns HTTP 200. Google credentials remain in user-secrets/ignored Compose configuration; the live challenge uses PKCE and `http://localhost:8080/signin-google`. The account owner connected Google and completed Google sign-in plus the application authenticator challenge; the vendor dashboard and permissions were observed.
- Browser verification found an unversioned SSO exchange URL and the equivalent new invitation-acceptance URL. Both now use configured versioned endpoints, with architecture tests comparing frontend configuration to backend route attributes. Browser verification also found no-tracking invitation writes reporting success without persistence. Revoke/retry now use tracked ID lookups, and reissue uses an explicitly tracked query; the provider tests reload records to verify persistence and retain revision concurrency checks.
- A disposable pooled tenant was created through the normal UI, remained pending KYC, then received a dedicated Mailpit welcome link after KYC approval/activation. The organization-specific welcome page showed password creation and Google acceptance options. Viewing it left the administrator list empty and setup awaiting acceptance. Its invitation was revoked and the tenant deactivated with normal audit behavior after verifying it had no identities; the link subsequently displayed a safe unavailable message with no acceptance form. The scoped test email and temporary maintenance operation were removed. The temporary browser tab was closed. Focused browser checks are not a formal accessibility-conformance audit; a new-account password/MFA journey was exercised by automated service/provider checks rather than entering new credentials through browser automation.

No StarterTemplate implementation, production migration/deployment, commit, or push is implied by these checks.
