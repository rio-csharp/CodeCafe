using CodeCafe.DbSync.Infrastructure;

namespace CodeCafe.DbSync.Commands;

internal sealed class LocalToTestCommand(
    SyncConfig config,
    ToolOptions options,
    ConsoleUi console,
    ProcessRunner processRunner,
    SshConnectionFactory sshFactory) : ICommand
{
    public void Run()
    {
        console.Heading("Local -> test sync");
        Console.WriteLine($"Local: {config.Local.Host}:{config.Local.Port}/{config.Local.DatabaseName}");
        Console.WriteLine($"Test:  {config.Test.Host}:{config.Test.DatabasePort}/{config.Test.DatabaseName}");
        Console.WriteLine();

        if (!console.Confirm($"This will overwrite test database '{config.Test.DatabaseName}'. Continue?", options.AssumeYes))
        {
            throw new OperationCanceledException();
        }

        processRunner.EnsureTool("pg_dump");

        var localPassword = console.PasswordFromEnvironmentOrPrompt(
            "LOCAL_DB_PASSWORD",
            "Local database password");

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var dumpFile = Path.Combine(Path.GetTempPath(), $"codecafe_local_{timestamp}.dump");
        var remoteDumpFile = $"/tmp/codecafe_local_{timestamp}.dump";
        var remoteBackupFile = $"{config.TestBackupDirectory.TrimEnd('/')}/codecafe_test_before_restore_{timestamp}.dump";

        try
        {
            console.Step("Dumping local database");
            processRunner.Run(
                "pg_dump",
                ProcessArguments.Join(
                    "-h", config.Local.Host,
                    "-p", config.Local.Port.ToString(),
                    "-U", config.Local.User,
                    "-d", config.Local.DatabaseName,
                    "-Fc",
                    "-f", dumpFile),
                environmentVariables: new Dictionary<string, string>
                {
                    ["PGPASSWORD"] = localPassword
                });
            console.Success(DescribeFile(dumpFile));

            console.Step("Uploading dump to test server");
            using var sftp = sshFactory.CreateSftpClient(config.Test, config.SshKeyPaths);
            sftp.Connect();
            using (var file = File.OpenRead(dumpFile))
            {
                sftp.UploadFile(file, remoteDumpFile, canOverride: true);
            }

            sftp.Disconnect();
            console.Success(remoteDumpFile);

            console.Step("Backing up and restoring test database");
            using var ssh = sshFactory.CreateSshClient(config.Test, config.SshKeyPaths);
            ssh.Connect();
            RunRemote(ssh, BuildRemoteRestoreCommand(remoteDumpFile, remoteBackupFile));
            ssh.Disconnect();
            console.Success($"Test database restored; previous state saved to {remoteBackupFile}");
        }
        finally
        {
            DeleteIfExists(dumpFile);
        }
    }

    private string BuildRemoteRestoreCommand(string remoteDumpFile, string remoteBackupFile)
    {
        var pgPassFile = ShellEscaping.SingleQuote(config.TestPgPassFile);
        var backupDir = ShellEscaping.SingleQuote(config.TestBackupDirectory);
        var backupFile = ShellEscaping.SingleQuote(remoteBackupFile);
        var dumpFile = ShellEscaping.SingleQuote(remoteDumpFile);
        var dbHost = ShellEscaping.SingleQuote(config.Test.DatabaseHost);
        var dbPort = config.Test.DatabasePort.ToString();
        var dbUser = ShellEscaping.SingleQuote(config.Test.DatabaseUser);
        var dbName = ShellEscaping.SingleQuote(config.Test.DatabaseName);

        var script = string.Join(" && ", new[]
        {
            "set -euo pipefail",
            $"export PGPASSFILE={pgPassFile}",
            $"test -f {pgPassFile}",
            $"mkdir -p {backupDir}",
            $"pg_dump -h {dbHost} -p {dbPort} -U {dbUser} -d {dbName} -Fc -f {backupFile}",
            $"dropdb -h {dbHost} -p {dbPort} -U {dbUser} --if-exists {dbName}",
            $"createdb -h {dbHost} -p {dbPort} -U {dbUser} {dbName}",
            $"pg_restore -h {dbHost} -p {dbPort} -U {dbUser} -d {dbName} --no-owner --no-privileges {dumpFile}",
            $"rm -f {dumpFile}"
        });

        return $"bash -lc {ShellEscaping.SingleQuote(script)}";
    }

    private static void RunRemote(Renci.SshNet.SshClient ssh, string commandText)
    {
        using var command = ssh.CreateCommand(commandText);
        var output = command.Execute();

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(output.Trim());
        }

        if (command.ExitStatus != 0)
        {
            throw new CommandException($"Remote restore failed with exit code {command.ExitStatus}:{Environment.NewLine}{command.Error}");
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
