using FluentValidation;

namespace CodeCafe.Application.Blocks.UpdateBlock;

public sealed class UpdateBlockCommandValidator : AbstractValidator<UpdateBlockCommand>
{
    public UpdateBlockCommandValidator()
    {
        // Revisions start at 1, so a BaseRevision below that can never match.
        RuleFor(command => command.BaseRevision).GreaterThanOrEqualTo(1);
    }
}
