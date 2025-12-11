using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Commands;

public record UpdateUserProfileCommand(string UserId, Dictionary<string, string> Preferences) : IRequest;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
        _ = RuleFor(c => c.Preferences).NotNull();
    }
}
