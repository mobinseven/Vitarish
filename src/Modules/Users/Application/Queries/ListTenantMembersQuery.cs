using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Queries;

public static class TenantMemberQueryDefaults
{
    public const int MaxResults = 50;
}

public sealed record ListTenantMembersQuery(string? Search, int MaxResults) : IRequest<IReadOnlyList<TenantMemberSummary>>;

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
    }
}
