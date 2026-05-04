namespace CodeCafe.Infrastructure.Notes;

using CodeCafe.Application.Notes;
using CodeCafe.Contracts.Notes;

public sealed class FileSystemNotesRepository(INotesSettingsRepository settingsRepository) : INotesRepository
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".md",
        ".markdown",
        ".txt"
    };

    public async Task<IReadOnlyCollection<NoteSummaryResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var rootPath = await GetRootPathAsync(cancellationToken);

        if (rootPath is null)
        {
            return Array.Empty<NoteSummaryResponse>();
        }

        return Directory
            .EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .Where(IsSupportedNoteFile)
            .Select(filePath => CreateSummary(rootPath, filePath))
            .OrderBy(note => note.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<NoteContentResponse?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        var rootPath = await GetRootPathAsync(cancellationToken);

        if (rootPath is null || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var fullPath = Path.GetFullPath(Path.Combine(rootPath, path));

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) || !IsSupportedNoteFile(fullPath) || !File.Exists(fullPath))
        {
            return null;
        }

        var info = new FileInfo(fullPath);
        var content = await File.ReadAllTextAsync(fullPath, cancellationToken);

        return new NoteContentResponse(
            ToRelativePath(rootPath, fullPath),
            Path.GetFileNameWithoutExtension(fullPath),
            info.LastWriteTimeUtc,
            info.Length,
            content);
    }

    private static NoteSummaryResponse CreateSummary(string rootPath, string filePath)
    {
        var info = new FileInfo(filePath);

        return new NoteSummaryResponse(
            ToRelativePath(rootPath, filePath),
            Path.GetFileNameWithoutExtension(filePath),
            info.LastWriteTimeUtc,
            info.Length);
    }

    private async Task<string?> GetRootPathAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(settings.RootPath))
        {
            return null;
        }

        var rootPath = Path.GetFullPath(settings.RootPath);

        return Directory.Exists(rootPath) ? Path.TrimEndingDirectorySeparator(rootPath) + Path.DirectorySeparatorChar : null;
    }

    private static bool IsSupportedNoteFile(string path)
    {
        return SupportedExtensions.Contains(Path.GetExtension(path));
    }

    private static string ToRelativePath(string rootPath, string filePath)
    {
        return Path.GetRelativePath(rootPath, filePath).Replace(Path.DirectorySeparatorChar, '/');
    }
}
