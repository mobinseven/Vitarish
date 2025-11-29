using MediatR;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Commands.UpdateUserProfile;

public record UpdateUserProfileCommand(string UserId, Dictionary<string, string> Preferences) : IRequest;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Preferences).NotNull();
    }
}

internal class UpdateUserProfileCommandHandler(UserManager<User> userManager) : IRequestHandler<UpdateUserProfileCommand>
{
    public async Task Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId) ?? throw new InvalidDataException("User ID is invalid");
        user.ProfilePreferences = request.Preferences;
        await userManager.UpdateAsync(user);
    }
}
