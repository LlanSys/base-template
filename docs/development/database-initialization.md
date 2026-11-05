# BaseTemplate database initialization

This runbook applies to BaseTemplate, the domain reference implementation. StarterTemplate is the neutral default for downstream applications and has its own database lifecycle. Do not copy BaseTemplate's domain contexts or migration history into StarterTemplate.

## Choose the operation

`tools/BT.DatabaseTools` supports SQL Server and PostgreSQL and includes all five BaseTemplate contexts: ControlPlane, IAM, Shared, HR, and Banking. It uses the same separate history-table names as the application. Supply the connection through `BT_DATABASE_CONNECTION` in the current process using your existing local configuration or secret store. Never put credentials in arguments, source control, or console output.

```powershell
# Set BT_DATABASE_CONNECTION securely for this process first.
dotnet run --project tools/BT.DatabaseTools -- SqlServer status
dotnet run --project tools/BT.DatabaseTools -- SqlServer fresh
# PostgreSQL uses the same commands with PostgreSql as the provider argument.
```

Use `fresh` only for a new, explicitly named application database. It rejects system databases, differing context connections, missing migrations, and model/snapshot drift. It creates the current EF schemas and seeds plus Quartz's provider-specific scheduler schema, then records the matching migration histories. All five schemas, seeds, scheduler tables, and history rows commit in one transaction. Failure rolls back those changes; creating the empty database itself is outside that transaction. A second run against a fully initialized database changes nothing. A partially populated or older database is refused, without being reset or dropped.

BaseTemplate's early IAM migrations contain seed updates before the schema baseline; historical PostgreSQL scheduler SQL also contains an invalid reset preamble. For that reason, new installations use this explicit current-model baseline instead of replaying every historical migration from zero. Previously applied migration files and identities remain intact for existing databases. Future migrations apply normally after initialization. Do not use `EnsureCreated` for application setup or manually insert history rows.

## Existing databases

Back up the selected database and check `status` before upgrading. Then run:

```powershell
dotnet run --project tools/BT.DatabaseTools -- SqlServer upgrade
# PostgreSql upgrade is also supported.
```

The upgrade operation refuses missing/empty databases and system databases, then applies outstanding migrations separately for each context. Context upgrades are not one global transaction: if a later context fails, fix the reported provider/configuration problem and rerun the idempotent upgrade. The tool never resets or drops a database. Errors expose only safe guidance and provider diagnostic codes, not raw connection/provider messages. Clear `BT_DATABASE_CONNECTION` after use.

`RetireLegacyControlPlanePermission` removes the retired `controlplane.manage` definition and its role and direct-user grants. Assign approved granular capabilities explicitly through IAM role administration. Use the pre-upgrade backup if a rollback needs the retired grants restored. See [SaaS administration](../architecture/saas-administration.md).

## Local authenticator continuity

`reprotect-local-authenticator` is a Windows-only, single-account maintenance operation for an existing local database and the established local Redis ring. It refuses remote database/Redis endpoints and accounts without an enabled authenticator. Run it as the original Windows user with the original DPAPI-protected keys available. Supply these process environment variables without printing their values:

- `BT_DATABASE_CONNECTION`: loopback application database connection.
- `BT_AUTHENTICATOR_USER_ID`: the affected existing account.
- `BT_LOCAL_SOURCE_KEY_DIRECTORY`: original `%LOCALAPPDATA%/BTApi/DataProtection-Keys` directory.
- `BT_LOCAL_REDIS_CONNECTION`: loopback connection to the existing BaseTemplate Redis instance.

```powershell
dotnet run --project tools/BT.DatabaseTools -- SqlServer reprotect-local-authenticator
```

The operation decrypts with the original ring and re-encrypts with the existing Redis ring using the same `BaseTemplate` application discriminator and `TotpSecrets` purpose. It verifies the plaintext is unchanged before committing, and updates only that account's existing active/pending authenticator ciphertext in one transaction. No master key, plaintext secret, password, or token is exported or logged. MFA enrollment and permissions are preserved. Repeating the operation is a no-op for ciphertext already readable by Redis. Clear the process variables afterward.

After a successful continuity repair, configure the local Visual Studio API to use the same ring as Docker (`UseExternalKeyStore=true`, empty local `BlobKeyUri`, the local Redis connection and key `BaseTemplate-DataProtection-Keys`, and local `KeyEncryptionMode=None`). The setup scripts expose this through explicit `-UseSharedLocalKeyRing`. Do not change `ApplicationName` or delete old keys. New current authenticator codes should then work without re-enrollment.

## Verification

`FreshDatabaseInitializationTests` runs both providers against disposable test databases. It covers all five current schemas, seeds, history consistency, subsequent migration execution, Quartz table availability, no-op reruns preserving operator data, refusal of partial databases, and rollback after an injected mid-initialization failure. `SaasAdministrationMigrationTests` separately verifies upgrades from the previous release model, preservation of queued outbox messages and legacy metadata through the MassTransit 8 pin, and invitation recovery. These tests do not imply a production migration or deployment.
