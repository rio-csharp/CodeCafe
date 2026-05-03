namespace CodeCafe.Api.Endpoints;

using CodeCafe.Application.Notes;

public static class NotesEndpoints
{
    public static IEndpointRouteBuilder MapNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notes")
            .WithTags("Notes");

        group.MapGet("/", async (
            INotesService service,
            CancellationToken cancellationToken) =>
        {
            var notes = await service.ListAsync(cancellationToken);

            return Results.Ok(notes);
        })
        .WithName("ListNotes");

        group.MapGet("/content", async (
            string path,
            INotesService service,
            CancellationToken cancellationToken) =>
        {
            var note = await service.ReadAsync(path, cancellationToken);

            return note is null ? Results.NotFound() : Results.Ok(note);
        })
        .WithName("ReadNote");

        return app;
    }
}
