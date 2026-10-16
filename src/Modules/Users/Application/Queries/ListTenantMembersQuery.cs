using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Queries;

public static class TenantMemberQueryDefaults
{
    public const int MaxResults = 50;
}

/// <param name="UserIds">When set, only these members are returned, matched exactly by ID.</param>
public sealed record ListTenantMembersQuery(string? Search, int MaxResults, IReadOnlyCollection<string>? UserIds = null) : IRequest<IReadOnlyList<TenantMemberSummary>>;

public sealed record TenantMemberSummary(
    string Id,
    string? UserName,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? FullName,
    string RoleSuffix);

public sealed class ListTenantMembersQueryValidator : AbstractValidator<ListTenantMembersQuery>
{
    public ListTenantMembersQueryValidator()
    {
        _ = RuleFor(query => query.Search).MaximumLength(200);
        _ = RuleFor(query => query.MaxResults).InclusiveBetween(1, 200);
        _ = RuleFor(query => query.UserIds!.Count).LessThanOrEqualTo(query => query.MaxResults).When(query => query.UserIds is not null);
    }
}
