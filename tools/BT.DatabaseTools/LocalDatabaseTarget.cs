using System.Net;
using System.Xml.Linq;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

using Npgsql;

namespace BT.DatabaseTools;

internal static class LocalDatabaseTarget
{
    public static string Resolve(string? connection, bool sqlServer)
    {
        if (string.IsNullOrWhiteSpace(connection))
        {
            connection = LoadDevelopmentConnection();
        }

        if (sqlServer)
        {
            var settings = new SqlConnectionStringBuilder(connection);
            var server = settings.DataSource;

            if (server.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
            {
                server = server[4..];
            }

            var host = server.Split([',', '\\'], 2)[0].Trim();
            var local = IsLoopback(host) || host == "." || string.Equals(host, "(local)", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "(localdb)", StringComparison.OrdinalIgnoreCase);

            if (!local || !string.IsNullOrWhiteSpace(settings.FailoverPartner)
                || !string.IsNullOrWhiteSpace(settings.AttachDBFilename))
            {
                throw new LocalDatabaseTargetException("Choose a local SQL Server endpoint without a failover partner or attached database file.");
            }
        }
        else if (!IsLoopback(new NpgsqlConnectionStringBuilder(connection).Host))
        {
            throw new LocalDatabaseTargetException("Choose a single loopback PostgreSQL host.");
        }

        return connection;
    }

    private static string LoadDevelopmentConnection()
    {
        var apiDirectory = Path.GetFullPath("src/Backend/Api/BT.Api");
        var project = Path.Combine(apiDirectory, "BT.Api.csproj");

        if (!File.Exists(project))
        {
            throw new LocalDatabaseTargetException("Run the local database command from the BaseTemplate repository root.");
        }

        var userSecretsId = XDocument.Load(project).Descendants("UserSecretsId").FirstOrDefault()?.Value;

        if (string.IsNullOrWhiteSpace(userSecretsId))
        {
            throw new LocalDatabaseTargetException("The API project must declare a UserSecretsId for local database configuration.");
        }

        using var configuration = (ConfigurationRoot)new ConfigurationBuilder()
            .SetBasePath(apiDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets(userSecretsId)
            .AddEnvironmentVariables()
            .Build();

        var connection = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connection)
            || connection.StartsWith("SET_VIA_", StringComparison.OrdinalIgnoreCase)
            || connection.StartsWith("REPLACE_VIA_", StringComparison.OrdinalIgnoreCase))
        {
            throw new LocalDatabaseTargetException("Set the API's ConnectionStrings:DefaultConnection in user secrets, or set BT_DATABASE_CONNECTION for this process.");
        }

        string[] contexts = ["ControlPlaneConnection", "IamConnection", "SharedConnection", "HrConnection", "BankingConnection"];

        foreach (var name in contexts)
        {
            if (configuration.GetConnectionString(name) is { } scoped
                && !string.Equals(scoped, connection, StringComparison.Ordinal))
            {
                throw new LocalDatabaseTargetException(
                    $"The API overrides {name}. This local workflow requires all five contexts to share one application database.");
            }
        }

        return connection;
    }

    private static bool IsLoopback(string? host)
    {
        host = host?.Trim('[', ']');
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
    }
}

internal sealed class LocalDatabaseTargetException(string message) : InvalidOperationException(message);
