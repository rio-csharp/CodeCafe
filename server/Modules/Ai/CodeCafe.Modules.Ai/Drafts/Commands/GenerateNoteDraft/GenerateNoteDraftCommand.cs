using CodeCafe.Application.Common.Messaging;

namespace CodeCafe.Modules.Ai.Drafts.Commands.GenerateNoteDraft;

public sealed record GenerateNoteDraftCommand(
    Guid ActorId,
    string NotebookSlug,
    string? ActivePagePath,
    string? Intent,
    string Prompt,
    string? Locale) : ICommand<GenerateNoteDraftResult>;
