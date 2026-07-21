using CodeCafe.DbSync.Infrastructure;
using Renci.SshNet;

namespace CodeCafe.DbSync.Commands;

internal sealed class MigrateEnvironmentCommand(
    EndpointConfig endpoint,
    SyncConfig config,
    string namespaceName,
    uint tunnelPort,
    ConsoleUi console,
    ProcessRunner processRunner,
    SshConnectionFactory sshFactory) : ICommand
{
    public void Run()
    {
        console.Heading($"Applying EF Core migrations to {namespaceName}");
        processRunner.EnsureTool("dotnet");

        using var ssh = sshFactory.CreateSshClient(endpoint, config.SshKeyPaths);
        using var forwardedPort = new ForwardedPortLocal("127.0.0.1", tunnelPort, endpoint.DatabaseHost, (uint)endpoint.DatabasePort);

        try
        {
            console.Step("Opening SSH tunnel");
            ssh.Connect();
            ssh.AddForwardedPort(forwardedPort);
            forwardedPort.Start();
            console.Success($"localhost:{tunnelPort} -> {endpoint.Host}:{endpoint.DatabasePort}");

            console.Step("Reading Kubernetes database secret");
            var remoteConnectionString = ReadRemoteConnectionString(ssh);
            var localConnectionString = ConnectionStringEditor.WithHostAndPort(
                remoteConnectionString,
                "127.0.0.1",
                (int)tunnelPort);
            console.Success(ConnectionStringEditor.MaskPassword(localConnectionString));

            console.Step("Running dotnet ef database update");
            processRunner.Run(
                "dotnet",
                ProcessArguments.Join(
                    "ef",
                    "database",
                    "update",
                    "--project",
                    "src/CodeCafe.Infrastructure/CodeCafe.Infrastructure.csproj",
                    "--startup-project",
                    "src/CodeCafe.Server/CodeCafe.Server.csproj",
                    "--context",
                    "ApplicationDbContext"),
                environmentVariables: new Dictionary<string, string>
                {
                    ["ConnectionStrings__DefaultConnection"] = localConnectionString,
                    ["ASPNETCORE_ENVIRONMENT"] = "Development"
                });
            console.Success("Migrations applied");
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
        }
    }

    private string ReadRemoteConnectionString(SshClient ssh)
    {
        var script = string.Join(" && ", new[]
        {
            "set -euo pipefail",
            "export KUBECONFIG=/etc/rancher/k3s/k3s.yaml",
            $"kubectl get secret codecafe-db-secret -n {ShellEscaping.SingleQuote(namespaceName)} -o jsonpath='{{.data.ConnectionStrings__DefaultConnection}}' | base64 -d"
        });

        using var command = ssh.CreateCommand($"bash -lc {ShellEscaping.SingleQuote(script)}");
        var output = command.Execute();
        if (command.ExitStatus != 0)
        {
            throw new CommandException($"Could not read database secret in namespace '{namespaceName}': {command.Error}");
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            throw new CommandException($"Database secret in namespace '{namespaceName}' is empty.");
        }

        return output.Trim();
    }
}
