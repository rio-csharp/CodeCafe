using CodeCafe.Modules.Ai.Common;
using CodeCafe.Shared.Application.Common.Abstractions.Messaging;

namespace CodeCafe.Modules.Ai.Edits.Commands.DiscardNotebookEditProposal;

public sealed record DiscardNotebookEditProposalCommand(
    Guid ProposalId,
    Guid ActorId) : ICommand<AiFlowError?>;
