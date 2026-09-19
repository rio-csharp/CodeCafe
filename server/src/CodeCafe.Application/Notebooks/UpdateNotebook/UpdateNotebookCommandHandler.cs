using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.UpdateNotebook;
using CodeCafe.Application.Notebooks.GetNotebookDetails;
namespace CodeCafe.Application.Notebooks.UpdateNotebook;

public sealed class UpdateNotebookCommandHandler : ICommandHandler<UpdateNotebookCommand, Result<NotebookDetailsDto>>
{
    public Task<Result<NotebookDetailsDto>> Handle(UpdateNotebookCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
