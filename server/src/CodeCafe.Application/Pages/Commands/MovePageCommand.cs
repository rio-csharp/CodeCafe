using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;
using CodeCafe.Application.Pages.Models;

namespace CodeCafe.Application.Pages.Commands;

public sealed record MovePageCommand(Guid PageId, string? NewParentPath, Guid? AfterPageId)
    : ICommand<Result<PageDetailsDto>>;
