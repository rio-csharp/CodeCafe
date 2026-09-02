using FluentValidation;
using MediatR;

namespace CodeCafe.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var requestValidators = validators as IValidator<TRequest>[] ?? validators.ToArray();
        if (requestValidators.Length != 0)
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(
                requestValidators.Select(validator => validator.ValidateAsync(context, cancellationToken))
            );

            var failures = validationResults.SelectMany(result => result.Errors).ToList();
            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next(cancellationToken);
    }
}
