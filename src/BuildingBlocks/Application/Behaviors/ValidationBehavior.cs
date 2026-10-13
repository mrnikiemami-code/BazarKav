using FluentValidation;
using MediatR;

namespace BazarKav.BuildingBlocks.Application.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        // Sequential: scoped validators may share a non-thread-safe persistence service.
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(new ValidationContext<TRequest>(request), cancellationToken);
            failures.AddRange(result.Errors.Where(failure => failure is not null));
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (failures.Count > 0) throw new ValidationException(failures);
        return await next(cancellationToken);
    }
}
