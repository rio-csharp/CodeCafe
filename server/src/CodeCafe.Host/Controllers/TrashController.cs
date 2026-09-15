using CodeCafe.Application.Common;
using CodeCafe.Application.Trash.Commands;
using CodeCafe.Application.Trash.Models;
using CodeCafe.Application.Trash.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.Controllers;

[ApiController]
[Route("api")]
[Tags("Trash")]
public sealed class TrashController(ISender sender) : ControllerBase
{
    [HttpGet("trash")]
    public Task<Result<CursorPage<TrashEntryDto>>> List(string? cursor, int? pageSize, CancellationToken cancellationToken)
        => sender.Send(new ListTrashQuery(cursor, pageSize), cancellationToken);

    [HttpPost("trash/{notebookId:guid}/restore")]
    public Task<Result> Restore(Guid notebookId, CancellationToken cancellationToken)
        => sender.Send(new RestoreNotebookFromTrashCommand(notebookId), cancellationToken);

    [HttpDelete("trash/{notebookId:guid}")]
    public Task<Result> Purge(Guid notebookId, CancellationToken cancellationToken)
        => sender.Send(new PurgeNotebookCommand(notebookId), cancellationToken);

    [HttpDelete("trash")]
    public Task<Result> EmptyTrash(CancellationToken cancellationToken)
        => sender.Send(new EmptyTrashCommand(), cancellationToken);
}
