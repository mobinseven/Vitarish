
using FluentValidation;
using MediatR;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Users.Application.Queries;

public record GetClientUserQuery(string ClientId, string UserId) : IRequest<UserDto?>;

public class GetClientUserQueryValidator : AbstractValidator<GetClientUserQuery>
{
    public GetClientUserQueryValidator()
    {
        _ = RuleFor(q => q.ClientId).NotEmpty();
        _ = RuleFor(q => q.UserId).NotEmpty();
    }
}
