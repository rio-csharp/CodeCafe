using CodeCafe.Application.Notes;
using CodeCafe.Contracts.Notes;

namespace CodeCafe.Api.Controllers;


[ApiController]
[Route("api/notes")]
[AllowAnonymous]
[Tags("Notes")]
public sealed class NotesController(INotesService service) : ControllerBase
{
    [HttpGet(Name = "ListNotes")]
    public async Task<ActionResult<IReadOnlyCollection<NoteSummaryResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var notes = await service.ListAsync(cancellationToken);

        return Ok(notes.Select(note => new NoteSummaryResponse(
            note.Path,
            note.Title,
            note.UpdatedAt,
            note.SizeBytes)).ToArray());
    }

    [HttpGet("content", Name = "ReadNote")]
    public async Task<ActionResult<NoteContentResponse>> ReadAsync(
        [FromQuery] string path,
        CancellationToken cancellationToken)
    {
        var note = await service.ReadAsync(path, cancellationToken);

        return note is null
            ? NotFound()
            : Ok(new NoteContentResponse(
                note.Path,
                note.Title,
                note.UpdatedAt,
                note.SizeBytes,
                note.Content));
    }
}
