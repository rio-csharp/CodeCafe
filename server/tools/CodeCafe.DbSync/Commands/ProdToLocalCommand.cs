using CodeCafe.DbSync.Infrastructure;
using Renci.SshNet;

namespace CodeCafe.DbSync.Commands;

internal sealed class ProdToLocalCommand(
    SyncConfig config,
    ToolOptions options,
    ConsoleUi console,
    ProcessRunner processRunner,
    SshConnectionFactory sshFactory) : ICommand
{
    public void Run()
    {
        console.Heading("Production -> local sync");
        Console.WriteLine($"Production: {config.Production.Host}:{config.Production.DatabasePort}/{config.Production.DatabaseName}");
        Console.WriteLine($"Local:      {config.Local.Host}:{config.Local.Port}/{config.Local.DatabaseName}");
        Console.WriteLine();

        if (!console.Confirm($"This will overwrite local database '{config.Local.DatabaseName}'. Continue?", options.AssumeYes))
        {
            throw new OperationCanceledException();
        }

        EnsureTools();

        var prodPassword = console.PasswordFromEnvironmentOrPrompt(
            "PROD_DB_PASSWORD",
            "Production database password");
        var localPassword = console.PasswordFromEnvironmentOrPrompt(
            "LOCAL_DB_PASSWORD",
            "Local database password");

        var dumpFile = Path.Combine(Path.GetTempPath(), $"codecafe_prod_{DateTime.UtcNow:yyyyMMdd_HHmmss}.dump");
        const uint tunnelPort = 15432;

        using var ssh = sshFactory.CreateSshClient(config.Production, config.SshKeyPaths);
        using var forwardedPort = new ForwardedPortLocal("127.0.0.1", tunnelPort, config.Production.DatabaseHost, (uint)config.Production.DatabasePort);

        try
        {
            console.Step("Opening SSH tunnel to production database");
            ssh.Connect();
            ssh.AddForwardedPort(forwardedPort);
            forwardedPort.Start();
            console.Success($"localhost:{tunnelPort} -> production:{config.Production.DatabasePort}");

            console.Step("Dumping production database");
            processRunner.Run(
                "pg_dump",
                ProcessArguments.Join(
                    "-h", "127.0.0.1",
                    "-p", tunnelPort.ToString(),
                    "-U", config.Production.DatabaseUser,
                    "-d", config.Production.DatabaseName,
                    "-Fc",
                    "-f", dumpFile),
                environmentVariables: new Dictionary<string, string>
                {
                    ["PGPASSWORD"] = prodPassword
                });
            console.Success(DescribeFile(dumpFile));

            console.Step("Recreating local database");
            var localEnvironment = new Dictionary<string, string>
            {
                ["PGPASSWORD"] = localPassword
            };

            processRunner.Run(
                "dropdb",
                ProcessArguments.Join(
                    "-h", config.Local.Host,
                    "-p", config.Local.Port.ToString(),
                    "-U", config.Local.User,
                    "--if-exists",
                    config.Local.DatabaseName),
                environmentVariables: localEnvironment);

            processRunner.Run(
                "createdb",
                ProcessArguments.Join(
                    "-h", config.Local.Host,
                    "-p", config.Local.Port.ToString(),
                    "-U", config.Local.User,
                    config.Local.DatabaseName),
                environmentVariables: localEnvironment);

            processRunner.Run(
                "pg_restore",
                ProcessArguments.Join(
                    "-h", config.Local.Host,
                    "-p", config.Local.Port.ToString(),
                    "-U", config.Local.User,
                    "-d", config.Local.DatabaseName,
                    "--no-owner",
                    "--no-privileges",
                    dumpFile),
                environmentVariables: localEnvironment);

            console.Success("Local database restored");
        }
        finally
        {
            if (forwardedPort.IsStarted)
            {
                forwardedPort.Stop();
            }

            if (ssh.IsConnected)
            {
                ssh.Disconnect();
            }

            DeleteIfExists(dumpFile);
        }
    }

    private void EnsureTools()
    {
        foreach (var tool in new[] { "pg_dump", "pg_restore", "createdb", "dropdb" })
        {
            processRunner.EnsureTool(tool);
        }
    }

    private static string DescribeFile(string path)
    {
        var size = new FileInfo(path).Length;
        return $"{Path.GetFileName(path)} ({size / 1024 / 1024} MB)";
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
