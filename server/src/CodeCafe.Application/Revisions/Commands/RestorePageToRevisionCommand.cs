using CodeCafe.Application.Common;
using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Application.Revisions.Commands;

public sealed record RestorePageToRevisionCommand(Guid PageId, DateTimeOffset AtUtc) : ICommand<Result>;
