using Renci.SshNet;
using Renci.SshNet.Common;

namespace CodeCafe.DbSync.Infrastructure;

internal sealed class SshConnectionFactory(ConsoleUi console)
{
    public SshClient CreateSshClient(EndpointConfig endpoint, IReadOnlyList<string> keyPaths)
    {
        return new SshClient(CreateConnectionInfo(endpoint, keyPaths));
    }

    public SftpClient CreateSftpClient(EndpointConfig endpoint, IReadOnlyList<string> keyPaths)
    {
        return new SftpClient(CreateConnectionInfo(endpoint, keyPaths));
    }

    private ConnectionInfo CreateConnectionInfo(EndpointConfig endpoint, IReadOnlyList<string> keyPaths)
    {
        var methods = new List<AuthenticationMethod>();
        methods.AddRange(CreatePrivateKeyMethods(endpoint.SshUser, keyPaths));

        var keyboard = new KeyboardInteractiveAuthenticationMethod(endpoint.SshUser);
        keyboard.AuthenticationPrompt += (_, args) =>
        {
            foreach (var prompt in args.Prompts)
            {
                prompt.Response = console.PromptPassword($"SSH {endpoint.SshUser}@{endpoint.Host} {prompt.Request}");
            }
        };
        methods.Add(keyboard);

        var password = Environment.GetEnvironmentVariable(PasswordEnvironmentVariable(endpoint));
        if (!string.IsNullOrEmpty(password))
        {
            methods.Add(new PasswordAuthenticationMethod(endpoint.SshUser, password));
        }

        return new ConnectionInfo(endpoint.Host, endpoint.SshPort, endpoint.SshUser, methods.ToArray())
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private static string PasswordEnvironmentVariable(EndpointConfig endpoint)
    {
        return endpoint.SshPort == 22 ? "PROD_SSH_PASSWORD" : "TEST_SSH_PASSWORD";
    }

    private static IEnumerable<AuthenticationMethod> CreatePrivateKeyMethods(string user, IReadOnlyList<string> keyPaths)
    {
        foreach (var path in keyPaths.Where(File.Exists))
        {
            PrivateKeyFile keyFile;
            try
            {
                keyFile = new PrivateKeyFile(path);
            }
            catch (SshPassPhraseNullOrEmptyException)
            {
                continue;
            }
            catch (SshException)
            {
                continue;
            }

            yield return new PrivateKeyAuthenticationMethod(user, keyFile);
        }
    }
}
