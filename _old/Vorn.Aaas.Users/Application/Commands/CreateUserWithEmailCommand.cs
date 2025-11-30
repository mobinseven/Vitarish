
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Extensions;

namespace Vorn.Aaas.Users.Application.Commands;

public record CreateUserWithEmailCommand(string Email, string? FirstName, string? LastName) : IRequest<User?>;

public class CreateUserWithEmailCommandValidator : AbstractValidator<CreateUserWithEmailCommand>
{
    public CreateUserWithEmailCommandValidator()
    {
        _ = RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}

internal class CreateUserWithEmailCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<CreateUserWithEmailCommand, User?>
{
    public async Task<User?> Handle(CreateUserWithEmailCommand request, CancellationToken cancellationToken)
    {
        User? user = await userManager.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        user ??= await userManager.CreateUserWithoutPasswordAsync(
            request.Email,
            request.FirstName,
            request.LastName,
            u => userManager.SetEmailAsync(u, request.Email));

        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        return user;
    }
}
