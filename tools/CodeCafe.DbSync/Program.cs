using CodeCafe.DbSync.Commands;
using CodeCafe.DbSync.Infrastructure;

namespace CodeCafe.DbSync;

internal static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Usage.Print();
            return args.Length == 0 ? 1 : 0;
        }

        var options = ToolOptions.Parse(args.Skip(1));
        var config = SyncConfig.FromEnvironment();
        var console = new ConsoleUi();
        var processRunner = new ProcessRunner();
        var sshFactory = new SshConnectionFactory(console);

        try
        {
            ICommand command = args[0] switch
            {
                "check" => new CheckCommand(config, console, processRunner, sshFactory),
                "migrate-prod" => new MigrateEnvironmentCommand(
                    config.Production,
                    config,
                    "codecafe-prod",
                    15440,
                    console,
                    processRunner,
                    sshFactory),
                "migrate-test" => new MigrateEnvironmentCommand(
                    config.Test,
                    config,
                    "codecafe-test",
                    15441,
                    console,
                    processRunner,
                    sshFactory),
                "prod-to-local" => new ProdToLocalCommand(config, options, console, processRunner, sshFactory),
                "local-to-test" => new LocalToTestCommand(config, options, console, processRunner, sshFactory),
                _ => throw new CommandException($"Unknown command '{args[0]}'.")
            };

            command.Run();
            return 0;
        }
        catch (OperationCanceledException)
        {
            console.Warning("Aborted.");
            return 1;
        }
        catch (CommandException exception)
        {
            console.Error(exception.Message);
            return 1;
        }
        catch (Exception exception)
        {
            console.Error($"Unexpected failure: {exception.Message}");
            return 1;
        }
    }
}
