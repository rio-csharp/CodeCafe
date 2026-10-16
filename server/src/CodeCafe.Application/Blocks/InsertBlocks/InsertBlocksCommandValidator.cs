using FluentValidation;

namespace CodeCafe.Application.Blocks.InsertBlocks;

public sealed class InsertBlocksCommandValidator : AbstractValidator<InsertBlocksCommand>
{
    // One batch stays small enough for a single transaction and a single response.
    public const int MaxBlocksPerInsert = 100;

    public InsertBlocksCommandValidator()
    {
        RuleFor(command => command.Blocks.Count).InclusiveBetween(1, MaxBlocksPerInsert);
        RuleForEach(command => command.Blocks).ChildRules(block => block.RuleFor(input => input.Type).NotEmpty());
    }
}
