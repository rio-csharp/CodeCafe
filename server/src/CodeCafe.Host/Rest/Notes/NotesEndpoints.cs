namespace CodeCafe.Host.Rest.Notes;

public static partial class NotesEndpoints
{
    public static IEndpointRouteBuilder MapNotesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/notes").WithTags("Notes");

        MapPublicEndpoints(group);
        MapNotebookEndpoints(group);
        MapFavoriteEndpoints(group);
        MapItemEndpoints(group);

        return endpoints;
    }
}
