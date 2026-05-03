namespace CodeCafe.Domain.Notes;

public sealed class NotesSettings
{
    public NotesSettings(string rootPath)
    {
        RootPath = rootPath.Trim();
    }

    public string RootPath { get; private set; }

    public void UpdateRootPath(string rootPath)
    {
        RootPath = rootPath.Trim();
    }
}
