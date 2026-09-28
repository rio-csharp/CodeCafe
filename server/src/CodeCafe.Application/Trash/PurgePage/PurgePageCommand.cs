using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Trash.PurgePage;

public sealed record PurgePageCommand(Guid PageId) : ICommand<Result>;
