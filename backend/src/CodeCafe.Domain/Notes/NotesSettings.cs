namespace CodeCafe.Domain.Notes;

public sealed class NotesSettings
{
    public NotesSettings(string rootPath)
    {
        RootPath = NormalizeRootPath(rootPath);
    }

    public string RootPath { get; private set; }

    public void UpdateRootPath(string rootPath)
    {
        RootPath = NormalizeRootPath(rootPath);
    }

    private static string NormalizeRootPath(string rootPath)
    {
        ArgumentNullException.ThrowIfNull(rootPath);

        return rootPath.Trim();
    }
}
