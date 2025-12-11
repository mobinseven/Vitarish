
using FluentValidation;
using MediatR;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.ApiClients.Application.Commands;
/// <summary>
/// Command to edit an existing <see cref="ApiClient"/>.
/// </summary>
public sealed record UpdateApiClient(string Id, string Secret, string Scopes, string HostUri) : IRequest<ApiClient?>;

/// <summary>
/// Validator for <see cref="UpdateApiClient"/>.
/// </summary>
public class UpdateApiClientValidator : AbstractValidator<UpdateApiClient>
{
    public UpdateApiClientValidator()
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Secret).NotEmpty();
        _ = RuleFor(c => c.Scopes).NotEmpty();
        _ = RuleFor(c => c.HostUri).NotEmpty();
    }
}
