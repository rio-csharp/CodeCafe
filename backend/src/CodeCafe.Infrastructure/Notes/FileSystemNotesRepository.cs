namespace CodeCafe.Infrastructure.Notes;


public sealed class FileSystemNotesRepository(INotesSettingsRepository settingsRepository) : INotesRepository
{
    public async Task<IReadOnlyCollection<NoteSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var rootPath = await GetRootPathAsync(cancellationToken);

        if (rootPath is null)
        {
            return Array.Empty<NoteSummary>();
        }

        return Directory
            .EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
            .Where(FileSystemNoteFilePolicy.IsSupportedNoteFile)
            .Select(filePath => CreateSummary(rootPath, filePath))
            .OrderBy(note => note.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<NoteContent?> ReadAsync(string path, CancellationToken cancellationToken)
    {
        var rootPath = await GetRootPathAsync(cancellationToken);

        if (rootPath is null)
        {
            return null;
        }

        var fullPath = FileSystemNoteFilePolicy.ResolveReadableNotePath(rootPath, path);
        if (fullPath is null)
        {
            return null;
        }

        var info = new FileInfo(fullPath);
        var content = await File.ReadAllTextAsync(fullPath, cancellationToken);

        return new NoteContent(
            ToRelativePath(rootPath, fullPath),
            Path.GetFileNameWithoutExtension(fullPath),
            info.LastWriteTimeUtc,
            info.Length,
            content);
    }

    private static NoteSummary CreateSummary(string rootPath, string filePath)
    {
        var info = new FileInfo(filePath);

        return new NoteSummary(
            ToRelativePath(rootPath, filePath),
            Path.GetFileNameWithoutExtension(filePath),
            info.LastWriteTimeUtc,
            info.Length);
    }

    private async Task<string?> GetRootPathAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);

        return FileSystemNoteFilePolicy.GetExistingRootPath(settings.RootPath);
    }

    private static string ToRelativePath(string rootPath, string filePath)
    {
        return FileSystemNoteFilePolicy.ToRelativePath(rootPath, filePath);
    }
}
