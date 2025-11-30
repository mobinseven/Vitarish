
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Infrastructure.Extensions;

namespace Vorn.Aaas.Users.Application.Commands;

public record CreateUserWithPhoneCommand(string PhoneNumber, string? FirstName, string? LastName) : IRequest<User?>;

public class CreateUserWithPhoneCommandValidator : AbstractValidator<CreateUserWithPhoneCommand>
{
    public CreateUserWithPhoneCommandValidator()
    {
        _ = RuleFor(c => c.PhoneNumber).NotEmpty();
    }
}

internal class CreateUserWithPhoneCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<CreateUserWithPhoneCommand, User?>
{
    public async Task<User?> Handle(CreateUserWithPhoneCommand request, CancellationToken cancellationToken)
    {
        User? user = await userManager.Users.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);
        user ??= await userManager.CreateUserWithoutPasswordAsync(
            request.PhoneNumber,
            request.FirstName,
            request.LastName,
            u => userManager.SetPhoneNumberAsync(u, request.PhoneNumber));

        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        return user;
    }
}
