using CodeCafe.Domain.Notes;

namespace CodeCafe.UnitTests.Domain.Notes;

public sealed class NotesSettingsTests
{
    [Fact]
    public void Constructor_trims_root_path()
    {
        var settings = new NotesSettings("  /srv/notes  ");

        Assert.Equal("/srv/notes", settings.RootPath);
    }

    [Fact]
    public void Constructor_rejects_null_root_path()
    {
        Assert.Throws<ArgumentNullException>(() => new NotesSettings(null!));
    }

    [Fact]
    public void UpdateRootPath_trims_updated_value()
    {
        var settings = new NotesSettings("/srv/notes");

        settings.UpdateRootPath("  /srv/new-notes  ");

        Assert.Equal("/srv/new-notes", settings.RootPath);
    }

    [Fact]
    public void UpdateRootPath_rejects_null_root_path()
    {
        var settings = new NotesSettings("/srv/notes");

        Assert.Throws<ArgumentNullException>(() => settings.UpdateRootPath(null!));
    }
}
