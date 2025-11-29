using MediatR;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Commands;

public record AddUserPasswordCommand(string Id, string Password) : IRequest<IdentityResult>;

public class AddUserPasswordCommandValidator : AbstractValidator<AddUserPasswordCommand>
{
    public AddUserPasswordCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Password).NotEmpty();
    }
}

internal class AddUserPasswordCommandHandler(UserManager<User> userManager) : IRequestHandler<AddUserPasswordCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(AddUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.Id);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }
        return await userManager.AddPasswordAsync(user, request.Password);
    }
}
