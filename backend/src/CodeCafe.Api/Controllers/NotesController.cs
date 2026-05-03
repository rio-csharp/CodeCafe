namespace CodeCafe.Api.Controllers;

using CodeCafe.Application.Notes;
using CodeCafe.Contracts.Notes;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/notes")]
[Tags("Notes")]
public sealed class NotesController(INotesService service) : ControllerBase
{
    [HttpGet(Name = "ListNotes")]
    public async Task<ActionResult<IReadOnlyCollection<NoteSummaryResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var notes = await service.ListAsync(cancellationToken);

        return Ok(notes);
    }

    [HttpGet("content", Name = "ReadNote")]
    public async Task<ActionResult<NoteContentResponse>> ReadAsync(
        [FromQuery] string path,
        CancellationToken cancellationToken)
    {
        var note = await service.ReadAsync(path, cancellationToken);

        return note is null ? NotFound() : Ok(note);
    }
}
