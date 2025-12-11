using FluentValidation;
using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Commands;

public record CreateUserWithPhoneCommand(string PhoneNumber, string? FirstName, string? LastName) : IRequest<User?>;

public class CreateUserWithPhoneCommandValidator : AbstractValidator<CreateUserWithPhoneCommand>
{
    public CreateUserWithPhoneCommandValidator()
    {
        _ = RuleFor(c => c.PhoneNumber).NotEmpty();
    }
}
