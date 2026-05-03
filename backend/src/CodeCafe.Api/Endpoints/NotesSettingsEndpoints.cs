namespace CodeCafe.Api.Endpoints;

using CodeCafe.Application.Notes;
using CodeCafe.Contracts.Notes;

public static class NotesSettingsEndpoints
{
    public static IEndpointRouteBuilder MapNotesSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notes/settings")
            .WithTags("Notes Settings");

        group.MapGet("/", async (
            INotesSettingsService service,
            CancellationToken cancellationToken) =>
        {
            var settings = await service.GetAsync(cancellationToken);

            return Results.Ok(settings);
        })
        .WithName("GetNotesSettings");

        group.MapPut("/", async (
            UpsertNotesSettingsRequest request,
            INotesSettingsService service,
            CancellationToken cancellationToken) =>
        {
            var settings = await service.UpdateAsync(request, cancellationToken);

            return Results.Ok(settings);
        })
        .WithName("UpdateNotesSettings");

        return app;
    }
}
