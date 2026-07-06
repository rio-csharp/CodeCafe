using CodeCafe.DbSync.Infrastructure;
using Renci.SshNet.Common;

namespace CodeCafe.DbSync.Commands;

internal sealed class CheckCommand(
    SyncConfig config,
    ConsoleUi console,
    ProcessRunner processRunner,
    SshConnectionFactory sshFactory) : ICommand
{
    public void Run()
    {
        console.Heading("Checking local tools");
        foreach (var tool in new[] { "pg_dump", "pg_restore", "createdb", "dropdb", "psql" })
        {
            processRunner.EnsureTool(tool);
            console.Success(tool);
        }

        console.Heading("Checking SSH reachability");
        CheckSsh(config.Production, "production");
        CheckSsh(config.Test, "test");
    }

    private void CheckSsh(EndpointConfig endpoint, string label)
    {
        try
        {
            using var ssh = sshFactory.CreateSshClient(endpoint, config.SshKeyPaths);
            ssh.Connect();
            using var command = ssh.CreateCommand("printf ok");
            var output = command.Execute();
            ssh.Disconnect();

            if (command.ExitStatus != 0 || output.Trim() != "ok")
            {
                throw new CommandException($"{label} SSH command failed: {command.Error}");
            }

            console.Success($"{label} SSH {endpoint.SshUser}@{endpoint.Host}:{endpoint.SshPort}");
        }
        catch (SshAuthenticationException exception)
        {
            throw new CommandException($"{label} SSH authentication failed: {exception.Message}");
        }
    }
}
