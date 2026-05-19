using FluentValidation;
using MediatR;
using Vitarish.Users.Application.Common;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public record GetUsersByIdentificationCodeQuery(string IdentificationCode) : IRequest<IEnumerable<User>>;

public sealed class GetUsersByIdentificationCodeQueryValidator : AbstractValidator<GetUsersByIdentificationCodeQuery>
{
    public GetUsersByIdentificationCodeQueryValidator()
    {
        _ = RuleFor(q => q.IdentificationCode).NotEmpty().MaximumLength(UserIdentityNormalization.IdentificationCodeMaxLength);
    }
}
