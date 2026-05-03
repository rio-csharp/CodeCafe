namespace CodeCafe.Api.Controllers;

using CodeCafe.Application.Notes;
using CodeCafe.Contracts.Notes;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/notes/settings")]
[Tags("Notes Settings")]
public sealed class NotesSettingsController(INotesSettingsService service) : ControllerBase
{
    [HttpGet(Name = "GetNotesSettings")]
    public async Task<ActionResult<NotesSettingsResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await service.GetAsync(cancellationToken);

        return Ok(settings);
    }

    [HttpPut(Name = "UpdateNotesSettings")]
    public async Task<ActionResult<NotesSettingsResponse>> UpdateAsync(
        UpsertNotesSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var settings = await service.UpdateAsync(request, cancellationToken);

        return Ok(settings);
    }
}
