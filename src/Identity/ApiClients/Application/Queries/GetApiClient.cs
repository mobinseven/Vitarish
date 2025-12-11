
using FluentValidation;
using MediatR;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.ApiClients.Application.Queries;
/// <summary>
/// Query for a single <see cref="ApiClient"/> by identifier.
/// </summary>
public sealed record GetApiClient(string Id) : IRequest<ApiClient?>;

/// <summary>
/// Validator for <see cref="GetApiClient"/>.
/// </summary>
public sealed class GetApiClientValidator : AbstractValidator<GetApiClient>
{
    public GetApiClientValidator()
    {
        _ = RuleFor(q => q.Id).NotEmpty();
    }
}
