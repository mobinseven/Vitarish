using FluentValidation.Results;

namespace Vitarish.Entities.Server.Validation;
/// <summary>
/// MediatR pipeline behavior that validates requests using FluentValidation validators.
/// Throws EntityFailureException with validation errors if validation fails.
/// </summary>
/// <remarks>
/// <para>
/// <b>Thread Safety:</b> Validators are executed in parallel for better performance.
/// FluentValidation's built-in validators are thread-safe. Custom validators MUST be stateless
/// and thread-safe to work correctly in this pipeline.
/// </para>
/// <para>
/// Custom validators should not access shared state or external resources without proper synchronization.
/// If your custom validator requires shared state, implement proper locking or use sequential validation.
/// </para>
/// </remarks>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly bool _parallelExecution;

    /// <summary>
    /// Creates a new ValidationBehavior.
    /// </summary>
    /// <param name="validators">The validators to use.</param>
    /// <param name="parallelExecution">Whether to execute validators in parallel. Default is false (sequential).</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, bool parallelExecution = false)
    {
        _validators = validators;
        _parallelExecution = parallelExecution;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        ValidationContext<TRequest> context = new(request);

        ValidationResult[] validationResults;
        if (_parallelExecution)
        {
            try
            {
                // Execute validators in parallel for better performance
                // Note: FluentValidation validators are thread-safe for concurrent execution
                // Custom validators should also be stateless and thread-safe
                validationResults = await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            }
            catch (Exception ex)
            {
                throw new EntityFailureException(
                    EntityError.Validation(
                        "Validator threw an exception during parallel execution. " +
                        "Ensure all validators are stateless and thread-safe. See inner exception for details.",
                        new Dictionary<string, string>()),
                    ex);
            }
        }
        else
        {
            List<ValidationResult> results = [];
            foreach (IValidator<TRequest> v in _validators)
            {
                results.Add(await v.ValidateAsync(context, cancellationToken));
            }
            validationResults = results.ToArray();
        }

        List<ValidationFailure> failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        return failures.Count != 0
            ? throw new EntityFailureException(
                EntityError.Validation(
                    "One or more validation errors occurred.",
                    failures.ToDictionary(f => f.PropertyName, f => f.ErrorMessage)))
            : await next();
    }
}
