
using FluentValidation;

namespace Vitarish.ApiClients.Application.Queries;

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
