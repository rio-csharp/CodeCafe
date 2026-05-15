using System.Text.RegularExpressions;

namespace CodeCafe.Infrastructure.Notes;

internal static partial class FileSystemNoteFilePolicy
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md",
        ".markdown",
        ".txt"
    };

    public static string? GetExistingRootPath(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            return null;
        }

        var fullRootPath = Path.GetFullPath(rootPath);

        return Directory.Exists(fullRootPath)
            ? Path.TrimEndingDirectorySeparator(fullRootPath) + Path.DirectorySeparatorChar
            : null;
    }

    public static string? ResolveReadableNotePath(string rootPath, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(Path.Combine(rootPath, path));

        return IsInsideRoot(rootPath, fullPath) && IsSupportedNoteFile(fullPath) && File.Exists(fullPath)
            ? fullPath
            : null;
    }

    public static bool IsSupportedNoteFile(string path)
    {
        if (!SupportedExtensions.Contains(Path.GetExtension(path)))
        {
            return false;
        }

        return NumberedNotePrefixRegex().IsMatch(Path.GetFileNameWithoutExtension(path));
    }

    public static string ToRelativePath(string rootPath, string filePath)
    {
        return Path.GetRelativePath(rootPath, filePath).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static bool IsInsideRoot(string rootPath, string fullPath)
    {
        var relativePath = Path.GetRelativePath(rootPath, fullPath);

        return relativePath != ".."
            && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            && !Path.IsPathRooted(relativePath);
    }

    [GeneratedRegex(@"^\d{2}-")]
    private static partial Regex NumberedNotePrefixRegex();
}
