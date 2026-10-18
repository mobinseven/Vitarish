using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Commands;

/// <summary>
/// Marks the user's email or phone confirmed when it equals <paramref name="Contact"/>,
/// a destination the user just proved control of with a sign-in code. Returns whether
/// a contact was newly confirmed.
/// </summary>
public record ConfirmUserContactCommand(string UserId, string Contact) : IRequest<bool>;

public sealed class ConfirmUserContactCommandValidator : AbstractValidator<ConfirmUserContactCommand>
{
    public ConfirmUserContactCommandValidator()
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
        _ = RuleFor(c => c.Contact).NotEmpty();
    }
}
