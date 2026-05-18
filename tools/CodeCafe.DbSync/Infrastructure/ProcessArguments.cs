using System.Text;

namespace CodeCafe.DbSync.Infrastructure;

internal static class ProcessArguments
{
    public static string Join(params string[] args)
    {
        return string.Join(" ", args.Select(Quote));
    }

    public static string Quote(string value)
    {
        if (value.Length == 0)
        {
            return "\"\"";
        }

        if (!value.Any(char.IsWhiteSpace) && !value.Contains('"'))
        {
            return value;
        }

        var builder = new StringBuilder();
        builder.Append('"');

        foreach (var ch in value)
        {
            if (ch is '"' or '\\')
            {
                builder.Append('\\');
            }

            builder.Append(ch);
        }

        builder.Append('"');
        return builder.ToString();
    }
}
