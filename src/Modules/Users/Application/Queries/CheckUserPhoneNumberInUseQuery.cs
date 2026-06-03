using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Queries;

public record CheckUserPhoneNumberInUseQuery(string PhoneNumber, string? ExcludeUserId) : IRequest<bool>;

public class CheckUserPhoneNumberInUseQueryValidator : AbstractValidator<CheckUserPhoneNumberInUseQuery>
{
    public CheckUserPhoneNumberInUseQueryValidator()
    {
        _ = RuleFor(q => q.PhoneNumber).NotEmpty();
    }
}
