using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Notebooks.GetNotebookTree;

public sealed record GetNotebookTreeQuery(string NotebookIdOrSlug)
    : IQuery<Result<NotebookTreeDto>>;
