# BaseTemplate release readiness

Last reviewed: 2026-11-05. This records local evidence, not production approval.

| Item | Current state | Remaining work |
| --- | --- | --- |
| Release review | The release change set covers IAM, administration, onboarding, migrations, and configuration. | Review the PR and its CI results before merging. |
| Legacy permission retirement | The earlier local inventory found one role grant and one direct user grant. SQL Server and PostgreSQL IAM migrations retire the old definition and remove its assignments during upgrade. | Confirm an authorized administrator can manage assignments on each target. Review any custom menus that reference the retired key and assign the required granular permissions explicitly. |
| Dependency warnings | Restore conflicts were corrected by aligning duplicate references. MassTransit is consistently 8.5.12. PKCS and XML cryptography packages use 10.0.14. The all-project, transitive NuGet audit reports zero vulnerable package entries. | Keep auditing at release time. Compiler/analyzer warnings remain and are not classified as resolved by the dependency audit. |
| Production validation | Open. Existing evidence covers local containers, two database providers, and focused browser/email flows. | Validate target configuration and secrets, shared Data Protection persistence, database backup/upgrade/rollback, approved email delivery, Microsoft and Google callback registrations, tenant isolation, MFA, health, observability, and rollback in the intended deployment environment. No production migration or deployment has occurred. |

## Web frontend and build platforms

Blazor is the only shipped frontend. Its nine presentation interfaces now live in feature-owned folders inside the web project; transport DTOs remain in SharedKernel. The solution, container build, workflows, roadmap, and analyzer configuration follow this boundary. The full solution no longer requires mobile workloads.

`BT.DatabaseTools` now declares both supported platforms and maps the x64 solution configuration to x64, matching its backend references. Debug solution builds pass for both Any CPU and x64 without MSB3270. Architecture guardrails verify every project's platform mapping and the frontend's transport-only project dependency.

The generic Visual Studio message “The file or directory is corrupted and unreadable” could not be reproduced or tied to a path. Reads of 1,629 repository files succeeded. An enumeration/read check of 2,663 local IDE cache files found no corruption errors; five files were locked or otherwise inaccessible. These checks do not prove overall disk health. No IDE state, database, or shared package cache was deleted. If the message returns, capture the specific path or diagnostic build output before attempting targeted cache repair or filesystem diagnostics.

## Scheduler compatibility

MassTransit uses one shared `MassTransitVersion` property in `Directory.Build.props`, currently 8.5.12, referenced as an exact NuGet range by the API, Infrastructure, Persistence, and integration tests. A later API-only update to 9.2.5 reproduced NU1605 in the integration project. This was an actual project-version conflict. Exclude MassTransit and Quartz from bulk major-version updates: their approved lines depend on explicit licensing and scheduler migration decisions. To update MassTransit within the approved line, change the shared property, restore the full solution, and validate messaging/integration compatibility. Do not suppress NU1605 or upgrade tests to 9.x to hide a mixed graph.

The follow-up also corrected an API-only Microsoft.OpenApi 3.11.0 update: Microsoft.AspNetCore.OpenApi 10.0.14 declares a dependency on Microsoft.OpenApi `>= 2.12.2 && < 3.0.0`. The selected version is 2.13.0, the latest stable 2.x returned by the official NuGet feed during this check. NuGet's [version-range reference](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning#version-ranges) documents exact pins. Shared pins keep project references aligned; they cannot prevent a deliberate package update operation from rewriting project files. Reload externally changed projects in Visual Studio and avoid selecting unsupported major versions in Update All.

Follow-up verification: forced full-solution restore succeeds without NuGet warnings, and the full Debug solution build succeeds with zero errors. The four affected projects' restored graphs contain only MassTransit 8.5.12 packages and no NU1605 log entries. Existing compiler/analyzer warnings remain; the test-suite counts below describe the preceding frontend cleanup, not a rerun for this package-only correction.

The checkout contained an incomplete Quartz 4 package upgrade. It failed compilation and would also require a database migration. This cleanup retains Quartz 3.19.1 and the existing job schema/serializer to preserve scheduled data. Treat a Quartz 4 upgrade as a dedicated change with SQL Server/PostgreSQL migrations and queued-job compatibility verification; see the [official migration guide](https://www.quartz-scheduler.net/documentation/quartz-4.x/migration-guide.html).

The cryptography package updates exceed the fixed versions documented in the Microsoft [PKCS advisory](https://github.com/advisories/GHSA-846f-f764-76mx) and [XML advisory](https://github.com/advisories/GHSA-7hqp-7c5m-4vq6). Roslyn compiler/workspace packages are aligned to avoid exact-version dependency constraint warnings.

StarterTemplate remains unchanged by this cleanup. Its onboarding/KYC/SSO follow-up remains in the [parity checklist](../architecture/invitation-acceptance-and-starter-parity.md).

## Verification for this cleanup

- Full solution restore succeeds without NuGet warnings. Any CPU and x64 Debug solution builds succeed; no processor-architecture mismatch is suppressed.
- 86 architecture tests, 106 unit tests, and 14 focused integration tests pass. The integration selection includes tenant/API/MFA isolation, provider upgrades, and fresh initialization for SQL Server and PostgreSQL.
- Release API and Blazor container images build successfully. Only those two application services were recreated in the existing `cg-basetemplate` group. Readiness, liveness, and sign-in return HTTP 200; messaging starts and startup logs have no error markers.
- The running Blazor image contains no retired frontend assemblies. Repository source/configuration searches find no remaining retired-project references; obsolete sample assets, mobile workflow, analyzer exceptions, and stale web build copies were removed.
- Existing Compose groups remain `cg-basetemplate` (eight services) and `cg-startertemplate` (five services). No new permanent Docker group or database migration was introduced.
- Changes remain uncommitted. Suggested cleanup commit: `refactor: consolidate Blazor frontend and align build dependencies`.

## Async lifetime build correction

The subsequent compilation repair keeps transaction, Redis, service-provider, and DI-scope variables in their original types and assigns configured disposal to a separate lifetime variable. The database tool's conditional provider construction targets each bounded context's common base class before configuring disposal. This preserves commit/rollback, service resolution, and disposal order without suppressing the async analyzer. The convention and [Microsoft reference](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync#using-async-disposable) are recorded in [configuration/code conventions](configuration-code-conventions.md#async-resource-lifetime).

The reported controller/model-builder null checks, display/cache culture selection, and sealing diagnostics were corrected in code. A further nullable role-name dereference was replaced with equality against a pre-normalized key so the query remains translatable by both database providers. No authorization grant policy, application schema, or authenticator enrollment was changed, and the maintenance recovery operation was not executed.

Any CPU and x64 solution builds pass with zero errors; the reported warning locations are clear in both builds. Existing unrelated diagnostics remain (11 warnings in the full x64 build). The current verification passes 86 architecture tests, 106 unit tests, and 14 focused integration tests. Provider checks cover fresh initialization and rollback, upgrades, invitation/account-link transactions, and API/tenant/MFA boundaries on SQL Server and PostgreSQL. Isolated test containers were disposed; no local application database or authenticator data was changed. Suggested repair commit: `fix: preserve async resource types and validate boundary inputs`.

The subsequent CA1826 follow-up replaces both first-command LINQ lookups in the PostgreSQL migration generator with indexable list patterns, preserving empty-list and first-null handling. Persistence builds with zero errors and no CA1826 diagnostics at either site; all ten existing migration-generator registration guardrails pass. No migration was generated or applied. Suggested commit: `perf: access PostgreSQL migration commands without LINQ`.

## Using directives and TimeProvider adoption

The requested standards are now in `AGENTS.md` and [configuration/code conventions](configuration-code-conventions.md). The active IAM command handlers use short type names and descriptive aliases where the handler and entity names collide. Related IAM queries, module registration, Control Plane persistence declarations, and host/Razor imports follow the same pattern. Generated migration/seed values and string-based type contracts were retained.

BaseTemplate runtime clock reads now use injected `TimeProvider` instances for IAM, invitation and entitlement lifecycles, domain transitions, audit stamping, payment verification and reconciliation, retry delays, calendar validation, and Blazor date defaults. Domain operations receive explicit timestamps, and the outbox keeps the existing `OccurredAt` field. Persistence continues to use `DateTimeOffset`, `DateTime`, and `DateOnly` values. Direct design-time and tooling context construction defaults to the system clock.

The first adoption batch passed Any CPU and x64 builds and verified invitation and signed JWT expiry with a controllable clock. The follow-up passed the solution build, 109 unit tests, 86 architecture tests, and 16 focused integration tests. These checks cover entity and administration audit timestamps, module-revocation event timestamps, tenant isolation, database initialization and upgrades, and outbox serialization. The outbox tests also passed after the final module event update.

The [parity checklist](../architecture/invitation-acceptance-and-starter-parity.md) tracks StarterTemplate adoption of both conventions.
