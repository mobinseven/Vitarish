namespace Vitarish.Communications.Infrastructure.Messages.Behaviors;

using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Vitarish.Base.Domain.Results;

/// <summary>
/// MediatR pipeline behavior that validates requests using FluentValidation validators.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        
        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            return CreateValidationResult(failures);
        }

        return await next();
    }

    private static TResponse CreateValidationResult(List<ValidationFailure> failures)
    {
        var firstErrorMessage = failures.First().ErrorMessage;
        
        // If TResponse is a Result type, return a failure result
        if (typeof(TResponse).IsGenericType)
        {
            var genericType = typeof(TResponse).GetGenericTypeDefinition();
            if (genericType == typeof(Result<>))
            {
                var error = new Error("Validation.Failed", firstErrorMessage);
                
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(typeof(TResponse).GetGenericArguments()[0])
                    .GetMethod(nameof(Result.Failure), new[] { typeof(Error) });
                
                if (failureMethod == null)
                {
                    throw new InvalidOperationException($"Could not find Failure method on Result<{typeof(TResponse).GetGenericArguments()[0].Name}>");
                }
                
                var result = failureMethod.Invoke(null, new object[] { error });
                if (result == null)
                {
                    throw new InvalidOperationException("Failure method returned null");
                }
                
                return (TResponse)result;
            }
        }
        
        if (typeof(TResponse) == typeof(Result))
        {
            var error = new Error("Validation.Failed", firstErrorMessage);
            return (TResponse)(object)Result.Failure(error);
        }

        // For non-Result types, throw validation exception
        throw new ValidationException(failures);
    }
}
