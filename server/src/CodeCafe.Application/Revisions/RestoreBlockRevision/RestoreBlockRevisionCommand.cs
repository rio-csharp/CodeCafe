using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Revisions.RestoreBlockRevision;

public sealed record RestoreBlockRevisionCommand(Guid PageId, Guid BlockId, long BlockVersion)
    : ICommand<Result>;
