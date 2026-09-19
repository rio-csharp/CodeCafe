using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Notebooks.GetNotebookTree;

namespace CodeCafe.Application.Notebooks.GetNotebookTree;

public sealed record GetNotebookTreeQuery(string NotebookIdOrSlug)
    : IQuery<Result<NotebookTreeDto>>;
