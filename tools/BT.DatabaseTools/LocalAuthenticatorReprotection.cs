using System.Security.Cryptography;
using BT.Persistence.Features.IAM.DataContext;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BT.DatabaseTools;

/// <summary>Explicit, single-account continuity repair between existing local key stores.</summary>
internal static class LocalAuthenticatorReprotection
{
    public static async Task RunAsync(IamDBContext context, TimeProvider timeProvider)
    {
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("Run local DPAPI recovery as the original Windows user.");
        var databaseHost = context.Database.GetDbConnection() switch
        {
            Microsoft.Data.SqlClient.SqlConnection sql => sql.DataSource.Replace("tcp:", string.Empty, StringComparison.OrdinalIgnoreCase).Split(',')[0],
            Npgsql.NpgsqlConnection pg => pg.Host,
            _ => string.Empty
        };

        ArgumentNullException.ThrowIfNull(databaseHost, nameof(databaseHost));

        if (!IsLoopback(databaseHost)) throw new InvalidOperationException("Use an explicitly configured loopback application database.");
        var userId = Required("BT_AUTHENTICATOR_USER_ID");
        var sourceDirectory = Path.GetFullPath(Required("BT_LOCAL_SOURCE_KEY_DIRECTORY"));
        var redisOptions = ConfigurationOptions.Parse(Required("BT_LOCAL_REDIS_CONNECTION"));
        // This operation is deliberately limited to workstation stores, never deployed Redis hosts.
        if (redisOptions.EndPoints.Any(endpoint => endpoint switch
        {
            System.Net.DnsEndPoint dns => !IsLoopback(dns.Host),
            System.Net.IPEndPoint ip => !System.Net.IPAddress.IsLoopback(ip.Address),
            _ => true
        }))
            throw new InvalidOperationException("The target must be an existing loopback Redis key store.");

        if (!Directory.Exists(sourceDirectory)) throw new InvalidOperationException("The original key directory is unavailable.");
        var source = DataProtectionProvider.Create(new DirectoryInfo(sourceDirectory), builder =>
            builder.SetApplicationName("BaseTemplate").DisableAutomaticKeyGeneration())
            .CreateProtector("TotpSecrets");

        var redis = await ConnectionMultiplexer.ConnectAsync(redisOptions).ConfigureAwait(false);
        await using var redisLifetime = redis.ConfigureAwait(false);
        var services = new ServiceCollection().AddLogging();
        services.AddDataProtection().SetApplicationName("BaseTemplate").DisableAutomaticKeyGeneration()
            .PersistKeysToStackExchangeRedis(redis, "BaseTemplate-DataProtection-Keys");

        var provider = services.BuildServiceProvider();
        await using var providerLifetime = provider.ConfigureAwait(false);
        var target = provider.GetRequiredService<IDataProtectionProvider>().CreateProtector("TotpSecrets");

        var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false);
        await using var transactionLifetime = transaction.ConfigureAwait(false);
        var user = await context.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == userId && !user.IsDeleted).ConfigureAwait(false);

        if (!user.TwoFactorEnabled) throw new InvalidOperationException("This account has no enabled authenticator to preserve.");
        var secrets = await context.AppUserTotpSecrets.IgnoreQueryFilters()
            .Where(secret => secret.AppUserId == userId && secret.IsActive && !secret.IsDeleted).ToListAsync().ConfigureAwait(false);

        var now = timeProvider.GetUtcNow();
        var pending = await context.TempTotpSecrets.IgnoreQueryFilters()
            .Where(secret => secret.UserId == userId && !secret.IsDeleted && secret.ExpiresAt > now).ToListAsync().ConfigureAwait(false);

        if (secrets.Count == 0) throw new InvalidOperationException("The existing authenticator enrollment is unavailable.");
        var changed = 0;
        string Reprotect(string encrypted)
        {
            try { _ = target.Unprotect(encrypted); return encrypted; }
            catch (CryptographicException) { /* Expected when the source key belongs to the original store. */ }
            var plaintext = source.Unprotect(encrypted);
            var replacement = target.Protect(plaintext);

            if (!string.Equals(plaintext, target.Unprotect(replacement), StringComparison.Ordinal))
                throw new CryptographicException("Authenticator continuity validation failed.");
            changed++;
            return replacement;
        }

        foreach (var secret in secrets)
            context.Entry(secret).Property(secret => secret.EncryptedSecret).CurrentValue = Reprotect(secret.EncryptedSecret);

        foreach (var secret in pending)
            context.Entry(secret).Property(secret => secret.EncryptedSecret).CurrentValue = Reprotect(secret.EncryptedSecret);

        if (!string.IsNullOrWhiteSpace(user.TotpSecret)) user.TotpSecret = Reprotect(user.TotpSecret);

        if (changed != 0) await context.SaveChangesAsync().ConfigureAwait(false);
        await transaction.CommitAsync().ConfigureAwait(false);
        Console.WriteLine($"Reprotected {changed} existing authenticator records. Enrollment, MFA enforcement, and permissions are unchanged. No original key was exported.");
    }

    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException("Provide the explicit local recovery settings through the environment.");

    private static bool IsLoopback(string host) => string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        || System.Net.IPAddress.TryParse(host, out var address) && System.Net.IPAddress.IsLoopback(address);
}
