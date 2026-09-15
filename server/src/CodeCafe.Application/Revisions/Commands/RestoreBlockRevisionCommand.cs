using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Revisions.Commands;

public sealed record RestoreBlockRevisionCommand(Guid PageId, Guid BlockId, long Revision)
    : ICommand<Result>;
