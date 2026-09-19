using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

using CodeCafe.Application.Notebooks.GetNotebookDetails;
namespace CodeCafe.Application.Notebooks.CreateNotebook;

public sealed class CreateNotebookCommandHandler : ICommandHandler<CreateNotebookCommand, Result<NotebookDetailsDto>>
{
    public Task<Result<NotebookDetailsDto>> Handle(CreateNotebookCommand message, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
