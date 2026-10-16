using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Shared;

namespace CodeCafe.Application.Pages.MovePage;

public sealed record MovePageCommand(Guid PageId, string? NewParentPath, Guid? AfterPageId)
    : ICommand<Result<PageDetailsDto>>;
