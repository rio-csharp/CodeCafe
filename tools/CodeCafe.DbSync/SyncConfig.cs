namespace CodeCafe.DbSync;

internal sealed record SyncConfig(
    EndpointConfig Production,
    EndpointConfig Test,
    LocalDatabaseConfig Local,
    string TestPgPassFile,
    string TestBackupDirectory,
    string[] SshKeyPaths)
{
    public static SyncConfig FromEnvironment()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var defaultKeyPaths = new[]
        {
            Path.Combine(home, ".ssh", "id_ed25519"),
            Path.Combine(home, ".ssh", "id_rsa"),
            Path.Combine(home, ".ssh", "id_ecdsa")
        };

        return new SyncConfig(
            Production: new EndpointConfig(
                Host: RequiredEnv("PROD_HOST"),
                SshPort: EnvInt("PROD_SSH_PORT", 22),
                SshUser: Env("PROD_SSH_USER", "root"),
                DatabaseHost: "127.0.0.1",
                DatabasePort: EnvInt("PROD_DB_PORT", 5432),
                DatabaseUser: Env("PROD_DB_USER", "codecafe"),
                DatabaseName: Env("PROD_DB", "codecafe")),
            Test: new EndpointConfig(
                Host: RequiredEnv("TEST_HOST"),
                SshPort: EnvInt("TEST_SSH_PORT", 65008),
                SshUser: Env("TEST_SSH_USER", "root"),
                DatabaseHost: "localhost",
                DatabasePort: EnvInt("TEST_DB_PORT", 5432),
                DatabaseUser: Env("TEST_DB_USER", "codecafe"),
                DatabaseName: Env("TEST_DB", "codecafe")),
            Local: new LocalDatabaseConfig(
                Host: Env("LOCAL_HOST", "localhost"),
                Port: EnvInt("LOCAL_DB_PORT", 5432),
                User: Env("LOCAL_DB_USER", "codecafe"),
                DatabaseName: Env("LOCAL_DB", "codecafe")),
            TestPgPassFile: Env("TEST_PGPASSFILE", "/root/.pgpass"),
            TestBackupDirectory: Env("TEST_BACKUP_DIR", "/opt/backup/postgres"),
            SshKeyPaths: EnvList("SSH_KEY_PATHS", defaultKeyPaths));
    }

    private static string Env(string name, string fallback)
    {
        return string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))
            ? fallback
            : Environment.GetEnvironmentVariable(name)!;
    }

    private static string RequiredEnv(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CommandException($"Environment variable '{name}' is required.");
        }

        return value;
    }

    private static int EnvInt(string name, int fallback)
    {
        return int.TryParse(Environment.GetEnvironmentVariable(name), out var value)
            ? value
            : fallback;
    }

    private static string[] EnvList(string name, string[] fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value)
            ? fallback
            : value.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

internal sealed record EndpointConfig(
    string Host,
    int SshPort,
    string SshUser,
    string DatabaseHost,
    int DatabasePort,
    string DatabaseUser,
    string DatabaseName);

internal sealed record LocalDatabaseConfig(
    string Host,
    int Port,
    string User,
    string DatabaseName);
