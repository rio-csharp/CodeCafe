using CodeCafe.Contracts.Notes;
using System.Net;
using System.Net.Http.Json;

namespace CodeCafe.IntegrationTests;

public sealed class NotesEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Notes_can_be_listed_and_read_from_configured_root_path()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"codecafe-notes-{Guid.NewGuid():N}");
        var nestedPath = Path.Combine(rootPath, "career");
        Directory.CreateDirectory(nestedPath);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(nestedPath, "01-interview.md"), "# Interview\n\nRead-only note.");
            await File.WriteAllTextAsync(Path.Combine(rootPath, "ignore.json"), "{}");

            var client = factory.CreateClient();

            var settingsResponse = await client.PutAsJsonAsync("/api/notes/settings", new UpsertNotesSettingsRequest(rootPath));

            Assert.Equal(HttpStatusCode.OK, settingsResponse.StatusCode);

            var notes = await client.GetFromJsonAsync<IReadOnlyCollection<NoteSummaryResponse>>("/api/notes");

            Assert.NotNull(notes);

            var note = Assert.Single(notes);

            Assert.Equal("career/01-interview.md", note.Path);
            Assert.Equal("01-interview", note.Title);

            var content = await client.GetFromJsonAsync<NoteContentResponse>("/api/notes/content?path=career%2F01-interview.md");

            Assert.NotNull(content);
            Assert.Equal("career/01-interview.md", content.Path);
            Assert.Contains("Read-only note.", content.Content);
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Notes_reader_rejects_paths_outside_root()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"codecafe-notes-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);

        try
        {
            var client = factory.CreateClient();

            await client.PutAsJsonAsync("/api/notes/settings", new UpsertNotesSettingsRequest(rootPath));

            var response = await client.GetAsync("/api/notes/content?path=..%2Fsecret.md");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task Notes_ignore_markdown_files_without_number_prefix()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"codecafe-notes-{Guid.NewGuid():N}");
        var nestedPath = Path.Combine(rootPath, "career");
        Directory.CreateDirectory(nestedPath);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(rootPath, "README.md"), "# Ignore me");
            await File.WriteAllTextAsync(Path.Combine(nestedPath, "draft.md"), "# Draft");
            await File.WriteAllTextAsync(Path.Combine(nestedPath, "02-numbered.md"), "# Numbered");

            var client = factory.CreateClient();

            var settingsResponse = await client.PutAsJsonAsync("/api/notes/settings", new UpsertNotesSettingsRequest(rootPath));

            Assert.Equal(HttpStatusCode.OK, settingsResponse.StatusCode);

            var notes = await client.GetFromJsonAsync<IReadOnlyCollection<NoteSummaryResponse>>("/api/notes");

            Assert.NotNull(notes);
            var note = Assert.Single(notes);
            Assert.Equal("career/02-numbered.md", note.Path);

            var ignoredResponse = await client.GetAsync("/api/notes/content?path=career%2Fdraft.md");

            Assert.Equal(HttpStatusCode.NotFound, ignoredResponse.StatusCode);
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }
}
