using FluentValidation;
using MediatR;

namespace Vorn.Aaas.Host.Services;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidationBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            ValidationContext<TRequest> context = new(request);
            FluentValidation.Results.ValidationResult[] results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            List<FluentValidation.Results.ValidationFailure> failures = results.SelectMany(r => r.Errors).Where(f => f != null).ToList();
            if (failures.Count != 0)
            {
                logger.LogWarning("Validation failures for {Request}", typeof(TRequest).Name);
                throw new ValidationException(failures);
            }
        }
        return await next();
    }
}
