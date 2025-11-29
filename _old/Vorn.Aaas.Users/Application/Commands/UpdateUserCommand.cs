using MediatR;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Commands;

public record UpdateUserCommand(User User) : IRequest<IdentityResult>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(c => c.User).NotNull();
        RuleFor(c => c.User.Id).NotNull();
    }
}

internal class UpdateUserCommandHandler(UserManager<User> userManager) : IRequestHandler<UpdateUserCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        return await userManager.UpdateAsync(request.User);
    }
}
