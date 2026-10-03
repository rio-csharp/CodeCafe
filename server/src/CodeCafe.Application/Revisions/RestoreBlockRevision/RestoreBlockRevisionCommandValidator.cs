using FluentValidation;

namespace CodeCafe.Application.Revisions.RestoreBlockRevision;

public sealed class RestoreBlockRevisionCommandValidator : AbstractValidator<RestoreBlockRevisionCommand>
{
    public RestoreBlockRevisionCommandValidator()
    {
        // Versions start at 1.
        RuleFor(command => command.BlockVersion).GreaterThanOrEqualTo(1);
    }
}
