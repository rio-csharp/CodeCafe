using CodeCafe.Domain.Identity;
using FluentValidation;

namespace CodeCafe.Application.Auth.PersonalAccessTokens.CreatePersonalAccessToken;

public sealed class CreatePersonalAccessTokenCommandValidator : AbstractValidator<CreatePersonalAccessTokenCommand>
{
    public CreatePersonalAccessTokenCommandValidator()
    {
        // Matches the personal_access_tokens.name column length so oversized input is rejected
        // as a 400 instead of failing the insert with a 500.
        RuleFor(command => command.Name).NotEmpty().MaximumLength(PersonalAccessToken.MaxNameLength);

        RuleFor(command => command.ExpiresInDays)
            .InclusiveBetween(1, 365)
            .When(command => command.ExpiresInDays.HasValue);
    }
}
