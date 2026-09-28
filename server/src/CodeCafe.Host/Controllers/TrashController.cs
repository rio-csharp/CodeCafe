using CodeCafe.Application.Common;
using CodeCafe.Application.Trash.PurgeTrashedNotebooks;
using CodeCafe.Application.Trash.ListTrash;
using CodeCafe.Application.Trash.PurgeNotebook;
using CodeCafe.Application.Trash.PurgePage;
using CodeCafe.Application.Trash.RestoreNotebookFromTrash;
using CodeCafe.Application.Trash.RestorePageFromTrash;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CodeCafe.Host.Controllers;

[ApiController]
[Route("api")]
[Tags("Trash")]
public sealed class TrashController(ISender sender) : ControllerBase
{
    [HttpGet("trash")]
    public Task<Result<PagedResult<TrashEntryDto>>> List(int? page, int? pageSize, CancellationToken cancellationToken)
        => sender.Send(new ListTrashQuery(page, pageSize), cancellationToken);

    [HttpPost("trash/{notebookId:guid}/restore")]
    public Task<Result> Restore(Guid notebookId, CancellationToken cancellationToken)
        => sender.Send(new RestoreNotebookFromTrashCommand(notebookId), cancellationToken);

    [HttpDelete("trash/{notebookId:guid}")]
    public Task<Result> Purge(Guid notebookId, CancellationToken cancellationToken)
        => sender.Send(new PurgeNotebookCommand(notebookId), cancellationToken);

    // The /pages/ segment keeps these routes clear of the trash/{notebookId:guid} ones.
    [HttpPost("trash/pages/{pageId:guid}/restore")]
    public Task<Result> RestorePage(Guid pageId, CancellationToken cancellationToken)
        => sender.Send(new RestorePageFromTrashCommand(pageId), cancellationToken);

    [HttpDelete("trash/pages/{pageId:guid}")]
    public Task<Result> PurgePage(Guid pageId, CancellationToken cancellationToken)
        => sender.Send(new PurgePageCommand(pageId), cancellationToken);

    [HttpDelete("trash")]
    public Task<Result> PurgeTrashedNotebooks(CancellationToken cancellationToken)
        => sender.Send(new PurgeTrashedNotebooksCommand(), cancellationToken);
}
