using MediatR;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Commands;

public record SetUserPhoneNumberCommand(string Id, string PhoneNumber) : IRequest<IdentityResult>;

public class SetUserPhoneNumberCommandValidator : AbstractValidator<SetUserPhoneNumberCommand>
{
    public SetUserPhoneNumberCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.PhoneNumber).NotEmpty();
    }
}

internal class SetUserPhoneNumberCommandHandler(UserManager<User> userManager) : IRequestHandler<SetUserPhoneNumberCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(SetUserPhoneNumberCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.Id);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }
        return await userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
    }
}
