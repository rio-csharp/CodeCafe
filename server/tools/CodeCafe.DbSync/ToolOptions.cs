namespace CodeCafe.DbSync;

internal sealed class ToolOptions
{
    public bool AssumeYes { get; private init; }

    public static ToolOptions Parse(IEnumerable<string> args)
    {
        var options = new ToolOptions();

        foreach (var arg in args)
        {
            switch (arg)
            {
                case "--yes":
                case "-y":
                    options = options.WithAssumeYes();
                    break;
                default:
                    throw new CommandException($"Unknown option '{arg}'.");
            }
        }

        return options;
    }

    private ToolOptions WithAssumeYes()
    {
        return new ToolOptions { AssumeYes = true };
    }
}
