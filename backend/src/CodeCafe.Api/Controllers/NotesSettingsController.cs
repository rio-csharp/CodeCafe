using CodeCafe.Api.Configuration;
using CodeCafe.Application.Notes;
using CodeCafe.Contracts.Notes;

namespace CodeCafe.Api.Controllers;


[ApiController]
[Route("api/notes/settings")]
[Authorize]
[Tags("Notes Settings")]
public sealed class NotesSettingsController(INotesSettingsService service) : ControllerBase
{
    [HttpGet(Name = "GetNotesSettings")]
    public async Task<ActionResult<NotesSettingsResponse>> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await service.GetAsync(cancellationToken);

        return Ok(new NotesSettingsResponse(settings.RootPath));
    }

    [Authorize(Policy = ApiPolicyNames.EditNotesSettings)]
    [HttpPut(Name = "UpdateNotesSettings")]
    public async Task<ActionResult<NotesSettingsResponse>> UpdateAsync(
        UpsertNotesSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var settings = await service.UpdateAsync(request.RootPath, cancellationToken);

        return Ok(new NotesSettingsResponse(settings.RootPath));
    }
}
