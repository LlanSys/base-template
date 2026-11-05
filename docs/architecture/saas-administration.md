# SaaS administration

Updated 2026-11-04. This document records the recovered vendor IAM and module redesign and its continuation on `codex/saas-administration`.

## Decisions and ownership

All tenants run one shared codebase. Specialized functionality is implemented once, registered in the vendor catalogs, and restricted by availability, module entitlement, permission, and record/business rules. A deployment stamp selects infrastructure and configuration, not a tenant-specific source branch.

The Control Panel is the vendor surface for tenants, deployment stamps, module definitions, menu definitions, the access catalog, protected default roles, and administration audit records. Tenant Admin Center exposes organization settings and users/access within the current organization. The profile menu belongs in the top navigation bar; Admin Center belongs at the bottom of tenant navigation.

`IsPlatformStaff` explicitly distinguishes platform identities. Vendor operations use explicit permission keys. The retired `controlplane.manage` key is reserved and existing databases remove it through `RetireLegacyControlPlanePermission`. The effective access check uses current database assignments on each API request.

## Authorization contract

`EffectiveAccessMiddleware` runs after authentication and before authorization. A request-scoped `EffectiveAccessService` reconstructs access from current persistent state. Its cache lasts for that request only.

Tenant access requires an active, non-deleted user and tenant, a token bound to that user's tenant, an active scoped role or direct permission grant, and an active permission definition. Business permissions additionally require the implemented module to be globally active and entitled to the tenant with no elapsed expiration. A known operation's built-in module requirement remains mandatory even if an additional catalog requirement is configured. Platform keys and contexts are excluded from tenant access, using case-insensitive comparisons.

Roles are permission groups. Custom role names can repeat across tenants. Shared default role names are reserved and default roles are vendor-managed. Legacy custom roles assigned across organizations are split by tenant during catalog initialization, retaining their grants and moving the relevant assignments. Platform roles and tenant roles are evaluated in their respective identity scope. Record isolation still comes from tenant query filters and feature-specific business rules.

For specialized permissions, `AvailableTenantId` restricts availability to one organization; null permits all otherwise entitled organizations. `RequiredModule` adds an implemented module requirement. These fields are availability constraints, not executable feature definitions: adding a catalog row does not implement an endpoint or screen. This release supports all-or-one-tenant permission availability, not arbitrary tenant cohorts or scheduled capability contracts.

## Delegation

Possessing a permission is distinct from being allowed to grant it. The vendor controls `Permission.IsTenantDelegatable` through the permission editor. New custom definitions default to false; the migration explicitly enables the established tenant permission seeds and leaves platform permissions non-delegatable.

Tenant permission writes require the operation's management permission, access to the target user/custom role in the same tenant, and both effective ownership and delegation approval for every requested grant. Role assignments use the same checks on the role's effective permissions, so a role cannot be used to bypass a delegation restriction. Unavailable module grants in shared default roles remain dormant until entitlement is available; authorization still filters them on each request. Existing non-delegatable grants can be removed, but a tenant permission editor cannot resubmit them as part of a replacement grant set. Vendor changes do not erase already-issued grants; the flag constrains delegation.

Identity assignment writes and their audit records commit or roll back together. Concurrent identity writes use Identity concurrency stamps. Tenant, deployment-stamp, and module editors use revision tokens. Audit records contain actor/record identifiers, action, time, and changed field names, never password, credential, or configuration values.

## Refresh and enforcement timing

Revoking a permission or entitlement, disabling a module, or suspending/deleting a tenant changes effective access on the next API request. An already-running request retains its request snapshot. Access checks are database-backed rather than dependent on token expiration or distributed cache eviction; the tradeoff is a bounded set of database queries on each authenticated request.

The UI refreshes its current-user snapshot through the existing session lifecycle (five-minute keep-alive interval in the supplied settings). Refresh notifies native Blazor authorization, reloads tenant navigation, and updates header/profile components. Organization-setting save/reset requests a refresh immediately. Other open circuits update on their next session refresh or page reload; no real-time push broadcast is claimed. Backend checks remain authoritative throughout that interval.

## Supported organization settings

| Key | Validation/default | Implemented effect |
| --- | --- | --- |
| `Organization.Name` | Nonblank text up to 120 characters; `BaseTemplate` | Organization header and dashboard name |
| `Organization.TimeZone` | Recognized system time zone; `UTC` | Dashboard organization time and last sign-in display |
| `Organization.Culture` | Specific culture; `en-KE` | Regional formatting used by organization date/count formatting helpers |
| `Organization.DateFormat` | `dd/MM/yyyy`, `MM/dd/yyyy`, or `yyyy-MM-dd`; `dd/MM/yyyy` | Organization date display format |

The catalog supplies labels, descriptions, validation, and defaults. Reset removes an override. Unknown legacy keys are preserved in storage but excluded from supported forms and provider reads. Technical credentials, deployment configuration, and security limits remain vendor/deployment settings. This is not a promise to reformat every historical business table or to apply unspecified approval settings.

## Migrations and validation

The recovered `SaasAdministration` migrations cover IAM, Control Plane, and Shared for SQL Server and PostgreSQL. Additive `TenantPermissionDelegation` IAM migrations introduce the delegation boundary without rewriting deployed data. The unreleased administration migration inserts the new HR navigation parent before reparenting existing links, and detaches custom children before removing retired seeded demo/foundation menus.

The PostgreSQL idempotent generator leaves migration-history table creation to the provider and avoids duplicating `IF NOT EXISTS`. Npgsql's [history implementation](https://github.com/npgsql/efcore.pg/blob/v10.0.3/src/EFCore.PG/Migrations/Internal/NpgsqlHistoryRepository.cs#L193) adds that clause in a subsequent pass. Index idempotency continues to belong to the configured SQL generator.

The upgrade tests build the schema represented by each context's final pre-administration migration, retain the IAM reference/menu seeds, insert representative existing records, mark that historical baseline, and execute the actual administration/delegation migrations. They verify preservation of custom menu children and active custom deployment stamps, module definitions, delegation defaults, and default-role initialization on both providers. This is an existing-database upgrade test, not certification of the entire historical migration chain from an empty database.

Known historical bootstrap limitation: IAM history contains seed updates before its table-creation baseline, so replaying every migration against an empty database fails. Historical PostgreSQL Control Plane seeds also omit required byte-array row versions. The new Control Plane mapping uses application-managed version bytes for PostgreSQL writes; historical bootstrap replay still needs a reviewed baseline repair. New-database provisioning needs a separately reviewed baseline repair; do not mark fresh-database migration replay as certified from these upgrade tests.

Acceptance checks:

- Custom role permissions work without a recognized role name; a familiar name without grants provides no bypass.
- Missing, expired, disabled, or revoked module availability blocks business API access.
- Foreign-tenant roles, target users, and token/tenant mismatches are rejected.
- Tenant-specific permissions and platform authority stay within scope.
- Permission ownership alone cannot grant delegation; role assignment cannot bypass vendor approval.
- Rejected identity writes roll back grants and audit records; audits omit secret values.
- Supported organization settings have observable effects and reset behavior.
- API/UI builds, unit/architecture tests, direct API tests, and both-provider upgrade tests are verified independently.

Verification on 2026-11-04:

| Check | Result |
| --- | --- |
| Required API build | Passed |
| Blazor build | Passed, analyzer warnings remain |
| Architecture suite, including endpoint retirement coverage and both-provider model/snapshot consistency | 81 passed |
| Unit suite | 72 passed |
| Targeted integration cases | 15 passed: both provider upgrades with invitation recovery, four IAM isolation cases, and nine API cases including role-assignment pagination |
| Working and staged patch whitespace checks | Passed |

The follow-up local certification below records browser and real Mailpit checks separately from automated tests. No production migration or deployment is implied by these checks.


## Six-item administration closure

The six follow-up implementation items are implemented. The later local follow-up uses the existing `cg-basetemplate` Compose group; its local database received the additive migrations without a reset. StarterTemplate remains the neutral downstream default and was not modified. BaseTemplate remains the business-domain reference.

### Vendor permissions and upgrade procedure

The `ControlPlane.Access` UI shell policy requires `platform_staff=true` and a supported platform permission. Individual routes add native operation policies; navigation, dashboard data, and actions use the same capabilities. Authenticated users who lack a page permission see an access-denied message instead of being sent back through sign-in.

| Capability | Permission |
| --- | --- |
| Tenant list, details and operational counts | `controlplane.view_tenants` |
| Tenant creation | `controlplane.create_tenants` |
| Tenant edit / soft delete | `controlplane.edit_tenants` / `controlplane.delete_tenants` |
| Tenant module assignments | `controlplane.manage_tenant_modules` |
| Administrator invitations and retries | `controlplane.manage_tenant_administrators` |
| KYC, activation, suspension and provisioning callbacks | `controlplane.manage_tenant_lifecycle` |
| Stamp migration | `controlplane.migrate_tenants` |
| Stamp viewing / changes | `controlplane.view_stamps` / `controlplane.manage_stamps` |
| Administration audit / impersonation | `controlplane.view_audit` / `controlplane.impersonate` |
| Module viewing / registration / publication and changes | `modules.view` / `modules.create` / `modules.manage` |

Registering an active module also requires publication authority; registration alone permits a draft. The existing roles/users/menu management permissions remain operation-specific.

The additive `CompleteVendorAdministration` migrations introduce granular permission definitions and durable invitations. The subsequent IAM retirement migration removes the old definition and its role and direct-user grants. Granular assignments carry forward. Before upgrading an existing installation, confirm a platform administrator has `roles.view` and `roles.manage_permissions`, then review which operations each vendor role needs. Assign those operations through **Control Panel → Roles and Permissions**. Give support roles the views they need, and approve any write permissions separately. Fresh installations seed the granular catalog and initialize the protected platform-administrator role.

Platform identities carry no `tenant_id` claim. After current access is verified, the API stores their identity persistence scope in server-owned request items so MFA/current-user lookups do not accidentally fall back to an unrelated default tenant. This value cannot be supplied by a client header.

### Permission retirement and module readiness

`ImplementedPermissions` lists shipped permission contracts. The API metadata guardrail rejects unregistered endpoint permissions. Shipped keys cannot be deleted or deactivated through catalog CRUD; changing their lifecycle requires a reviewed release. Unshipped definitions can be soft-retired only after menu references and role/direct-user claims have been removed. The record and its audit history remain available in storage.

A disabled module serves as a draft. Activation evaluates all shipped permissions for that module, an active default role covering them, and every applicable implemented route's active menu binding. The module list and detail view return the same actionable readiness issues. Evaluation batches permission, role-claim, and menu reads. Global disable and tenant-specific entitlement changes remain separate operations.

### Administrator onboarding and delivery guarantees

Tenant creation requires an initial administrator's email and name. The tenant, selected module grants, and invitation are saved as one Control Plane graph/save operation. Creation leaves the existing KYC/provisioning lifecycle intact and reports the invitation as pending rather than claiming onboarding is complete.

The invitation worker polls every 30 seconds and processes at most ten due active-tenant invitations per batch. Each invitation has a concurrency revision and five-minute lease. Pending/non-active tenants and soft-deleted tenants do not receive invitations. The IAM account and administrator-role assignment are idempotent and transactional. New accounts receive an undisclosed random password and use the password-recovery verification flow to establish their own password.

A failed email delivery leaves the account intact and records a sanitized failure with exponential retries capped at a one-hour interval. Vendor operators can explicitly retry or resend from Tenant → Administrators; a current lease prevents conflicting retry requests. Delivery is **at least once**: a crash after provider acceptance but before the delivered status is saved can produce a duplicate email. It does not require a duplicate account or role assignment. Provider tests simulate failure followed by success and verify one account, one assignment, two delivery attempts, and no automatic resend after completion on SQL Server and PostgreSQL.

### Relationship screens and operational work

- Tenant details: Overview, Modules (effective availability and expiration), Administrators (accounts, invitation state and retry), and permission-controlled Activity.
- Menu details: Overview, Access (roles satisfying the permission), and Availability.
- Role details: Overview, Permissions with the existing management dialog, and a cursor-paged Assigned Users list. The API applies role and tenant scope before fetching 51 rows and returning 50.
- Module details retain the shared Overview, Default Roles, Permissions, and Menus catalogs. Inaccessible tabs do not issue their underlying API requests.

Tenant activity filters all three audit sources before materialization, including identity changes made by platform actors. Actor display-name resolution is batched. Platform dashboard counts come from database aggregates for active/suspended tenants, KYC/provisioning queues, provisioning failures, pending/failed invitations, and enabled/disabled stamps. Invitation and stamp metrics require their corresponding permissions. The failure link opens the tenant list with the relevant status filter. No tenant business records or placeholder operational totals are used.

The remaining Blazor web `/counter` and `/weather` pages and their unused weather model were removed. Payment adapters and real payment routes remain.

Suggested commit message: `feat: complete scoped vendor administration and tenant onboarding`.

## Local follow-up verification, 2026-11-04

- SQL Server and PostgreSQL: current-model initialization of all five BaseTemplate contexts, Quartz schema availability, migration-history consistency, preservation of operator data on rerun, refusal of partial databases, and rollback after an injected failure passed. Existing-database upgrade and invitation retry/recovery checks also passed. See [database initialization](../development/database-initialization.md).
- The nine focused API cases passed. Four IAM tenant-role cases and two mutation checks passed on both providers; the mutation fixture now uses tenant roles because menus are a vendor-owned catalogue. In total, these are 19 distinct focused integration cases. A broader attempt was interrupted by Docker engine failures before the focused serial rerun; it is not recorded as a successful run.
- Unit suite: 72 passed. Architecture suite: 81 passed. API, Blazor, and database-tool builds passed; existing package/analyzer warnings remain. Container-backed integration tests now run serially and fixtures dispose their containers immediately.
- A real vendor sign-in and existing authenticator verification reached the dashboard. The original Windows key was available, so one account's existing authenticator ciphertext was reprotected for the existing Redis ring without resetting enrollment, disabling MFA, changing permissions, exporting a master key, or exposing a secret. A second maintenance run changed zero authenticator records. The local Visual Studio API now uses that same ring. The explicit setup-script switch and continuity procedure are in the local-platform/database runbooks.
- A synthetic pooled tenant was created through the UI, its administrator invitation remained pending until KYC approval/activation, and the worker delivered the invitation to the existing Mailpit instance. Its link used the tenant hostname. Browser-initiated recovery produced a real Mailpit code; normal API verification and initial password setup succeeded. The synthetic administrator completed MFA and signed in through that hostname, saw only Payments and the tenant Admin Center, and received HTTP 403 on the vendor API.
- Browser checks found unlabeled row actions/search fields, an unnamed mobile profile control, an unavailable HR department lookup from the vendor menu catalogue, and an unnecessary JavaScript call during static sign-in disposal. These were corrected. The user explicitly approved the [empty-shell/circuit route repair](blazor-route-authorization-fix.md). Direct vendor URLs now restore the existing session; anonymous requests reach sign-in; server responses contain no protected tenant/account content. The tenant fixture's direct API access remains HTTP 403. At a 390-pixel viewport, document width did not exceed the viewport and the wide tenant table remained inside its horizontal scroll container. Keyboard Tab moved from the labelled search field to the Status control. These are focused functional/accessibility checks, not a formal accessibility-conformance audit.
- Tenant Overview, Modules, Administrators and Activity loaded; delivery showed one account, one delivered invitation and one attempt. Module default-role, role assigned-user, and menu Access/Availability views loaded. A second disposable tenant identity confirmed the final authenticated browser negative case: its vendor deep link displayed access denied while the API returned HTTP 403. Both synthetic tenants were soft-deleted through the normal UI, stopping access while retaining audit records; the list returned to the two original tenants.
- The parallel `bt-certification` group, its disposable database and generated publish/configuration files, and thirteen containers left by the interrupted test run were removed. Both established Compose groups and their retained data volumes were preserved. Docker was restarted only after explicit user approval. No new persistent Docker group is required.

No commit, push, production migration, or production deployment was performed. The remaining isolated historical test container from 2026-10-30 predates this follow-up and was not removed.

## MassTransit pin and UI wording follow-up, 2026-11-04

- The user selected the open-source 8.x line after determining that a separate MassTransit license key was unavailable. All thirteen MassTransit package references across the three runtime projects and integration tests now use 8.5.12. Messaging defaults to enabled again. The existing MediatR community key is forwarded independently at container runtime from ignored local configuration.
- SQL Server and PostgreSQL received separate additive `PinMassTransit8Outbox` migrations. The former `BusName` metadata is retained; no queued message or column is deleted. Both upgrade cases verify a pre-existing queued message and its metadata survive. Outbox locking now follows the configured database provider.
- Follow-up checks: 73 unit tests, 81 architecture tests, two fresh-database initialization tests, two existing-database upgrade tests, two outbox persistence tests, and one real RabbitMQ outbox/consumer round trip passed. The transport test's temporary SQLite database and non-durable auto-delete queue are cleaned up by its fixture. Setup scripts passed PowerShell syntax parsing; Git whitespace checks passed. API, UI, and database-tool builds passed with existing package/analyzer warnings.
- The additive migration was applied only to the existing local BaseTemplate SQL database; all five contexts report zero pending migrations. Only the API and Blazor services were rebuilt/recreated in `cg-basetemplate`. The live API reports a started RabbitMQ bus, no startup errors or license failures, and HTTP 200 on `/health/live`; the Blazor sign-in endpoint returns HTTP 200. StarterTemplate infrastructure was preserved.
- Tenant and module confirmations now describe deletion and the resulting loss of access without exposing soft-delete or record-retention implementation details. The native authorization and API permission boundaries are unchanged.
- A readiness probe found the configured private profile-image blob container absent from the existing Azurite account. It was initialized with anonymous access disabled, preserving the existing Docker service and volume. API liveness and readiness subsequently both returned HTTP 200. No Azure resource was changed.

Changes remain uncommitted. No production migration or deployment was performed.

## Explicit acceptance and Google sign-in, 2026-11-05

The initial-administrator flow now uses an expiring, one-use welcome invitation instead of password recovery. The durable worker sends the invitation after mandatory KYC and activation without creating an identity. Explicit acceptance starts restricted account setup; final administrator assignment requires the active tenant, current invitation, verified authenticator enrollment, and signed application MFA evidence from the current session. Viewing a link does not accept it. Delivery and setup statuses are distinct, and interrupted setup can resume through sign-in.

Google OIDC joins the existing Microsoft provider. Existing accounts connect a provider through authenticated password confirmation plus external authentication. Email alone cannot bind accounts. Protected OIDC state binds the registered organization and approved frontend callback to a central public API origin. Hash-only database codes replace the former Redis-cached SSO token response, and exchange consumes each code once. Refresh preserves existing signed MFA evidence without creating it from enrollment.

Verification: 106 unit tests, 84 architecture tests, ten HTTP permission/tenant/MFA cases, two expanded provider upgrade cases and two fresh-database cases passed. Both provider upgrade cases also verify tracked revoke/resend/reissue persistence by reloading saved records. Builds and local API readiness passed; existing package/analyzer warnings remain. The account owner completed the real Google connection, Google sign-in and application authenticator challenge, reaching the vendor dashboard. The dedicated Mailpit welcome page was inspected for a disposable KYC-approved tenant; viewing left its account uncreated, and its closed invitation subsequently displayed a safe unavailable message. A browser-entered new-account password/MFA setup and live Microsoft consent were not repeated; the new-account lifecycle is covered by the automated provider checks. No formal accessibility-conformance audit is claimed.

The local SQL database received only the additive IAM and Control Plane migrations; all contexts have zero pending migrations. Only the established BaseTemplate app services were rebuilt, and StarterTemplate was not modified. The disposable tenant was deactivated with audit records after verifying it had no identities, its invitation was revoked, and its scoped test email and temporary maintenance operation were removed. Existing infrastructure/data volumes were preserved. No commit, push, production migration or deployment was performed.

See [the implementation contract and required StarterTemplate backlog](invitation-acceptance-and-starter-parity.md) and [Google/Microsoft setup and upgrade notes](../development/google-and-microsoft-sso.md). KYC is required in both templates; finish BaseTemplate first, then adapt the verified platform behavior to StarterTemplate's own revised IAM model and migrations.
