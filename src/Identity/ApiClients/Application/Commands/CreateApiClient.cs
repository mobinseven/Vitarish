
using FluentValidation;
using MediatR;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.ApiClients.Application.Commands;
/// <summary>
/// Command to create a new <see cref="ApiClient"/>.
/// </summary>
public sealed record CreateApiClient(string Id, string Secret, string Scopes, string HostUri) : IRequest<ApiClient?>;

/// <summary>
/// Validator for <see cref="CreateApiClient"/>.
/// </summary>
public class CreateApiClientValidator : AbstractValidator<CreateApiClient>
{
    public CreateApiClientValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Secret).NotEmpty();
        _ = RuleFor(c => c.Scopes).NotEmpty();
        _ = RuleFor(c => c.HostUri).NotEmpty();
    }
}
