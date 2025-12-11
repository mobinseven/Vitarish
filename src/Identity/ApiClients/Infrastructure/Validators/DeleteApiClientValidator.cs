
using FluentValidation;
using Vitarish.ApiClients.Application.Commands;

namespace Vitarish.ApiClients.Infrastructure.Validators;

/// <summary>
/// Validator for <see cref="DeleteApiClient"/>.
/// </summary>
public class DeleteApiClientValidator : AbstractValidator<DeleteApiClient>
{
    public DeleteApiClientValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
    }
}
