using MySqlConnector;

namespace DjMixOrganizer.Data;

// Builds the MySQL connection string from environment variables rather than
// a config file — matches the same `source .env` step already used for the
// `docker compose` commands in the README, so there's one place these
// values live, not two.
public static class DjMixConnectionString
{
    public static string FromEnvironment()
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "127.0.0.1",
            Port = uint.Parse(Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306"),
            Database = RequireEnv("MYSQL_DATABASE"),
            UserID = RequireEnv("MYSQL_USER"),
            Password = RequireEnv("MYSQL_PASSWORD"),
        };

        return builder.ConnectionString;
    }

    private static string RequireEnv(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException(
            $"Missing required environment variable: {name}. Did you run `source .env` before launching the app?");
}
