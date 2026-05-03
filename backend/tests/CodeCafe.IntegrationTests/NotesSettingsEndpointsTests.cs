using CodeCafe.Contracts.Notes;
using System.Net;
using System.Net.Http.Json;

namespace CodeCafe.IntegrationTests;

public sealed class NotesSettingsEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Notes_root_path_can_be_updated()
    {
        var client = factory.CreateClient();

        var initialSettings = await client.GetFromJsonAsync<NotesSettingsResponse>("/api/notes/settings");

        Assert.NotNull(initialSettings);
        Assert.Equal(string.Empty, initialSettings.RootPath);

        var updateResponse = await client.PutAsJsonAsync("/api/notes/settings", new UpsertNotesSettingsRequest(
            "/srv/codecafe/notes"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedSettings = await updateResponse.Content.ReadFromJsonAsync<NotesSettingsResponse>();

        Assert.NotNull(updatedSettings);
        Assert.Equal("/srv/codecafe/notes", updatedSettings.RootPath);
    }
}
