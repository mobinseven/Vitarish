using FluentValidation;
using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Commands;

public record CreateUserWithEmailCommand(string Email, string? FirstName, string? LastName) : IRequest<User?>;

public class CreateUserWithEmailCommandValidator : AbstractValidator<CreateUserWithEmailCommand>
{
    public CreateUserWithEmailCommandValidator()
    {
        _ = RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}
