using FluentValidation;

namespace CodeCafe.Application.Revisions.RestorePageToRevision;

public sealed class RestorePageToRevisionCommandValidator : AbstractValidator<RestorePageToRevisionCommand>
{
    public RestorePageToRevisionCommandValidator()
    {
        // A default timestamp would silently read as "before everything" and wipe the page.
        RuleFor(command => command.AtUtc).NotEqual(default(DateTimeOffset));
    }
}
