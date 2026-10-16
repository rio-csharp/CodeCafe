using FluentValidation;

namespace CodeCafe.Application.Blocks.UpdateBlock;

public sealed class UpdateBlockCommandValidator : AbstractValidator<UpdateBlockCommand>
{
    public UpdateBlockCommandValidator()
    {
        // Versions start at 1, so a BaseVersion below that can never match.
        RuleFor(command => command.BaseVersion).GreaterThanOrEqualTo(1);
    }
}
