
using FluentValidation;
using MediatR;

namespace Vitarish.ApiClients.Application.Commands;
/// <summary>
/// Command to remove an API client by identifier.
/// </summary>
public sealed record DeleteApiClient(string Id) : IRequest;

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
