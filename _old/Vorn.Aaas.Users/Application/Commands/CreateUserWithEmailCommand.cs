namespace Vorn.Aaas.Users.Application.Commands;

using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Users.Application.Extensions;
using Vorn.Aaas.Users.Domain.Entities;

public record CreateUserWithEmailCommand(string Email, string? FirstName, string? LastName) : IRequest<User?>;

public class CreateUserWithEmailCommandValidator : AbstractValidator<CreateUserWithEmailCommand>
{
    public CreateUserWithEmailCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
    }
}

internal class CreateUserWithEmailCommandHandler(UserManager<User> userManager) : IRequestHandler<CreateUserWithEmailCommand, User?>
{
    public async Task<User?> Handle(CreateUserWithEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        return user is not null
            ? user
            : await userManager.CreateUserWithoutPasswordAsync(
                request.Email,
                request.FirstName,
                request.LastName,
                u => userManager.SetEmailAsync(u, request.Email));
    }
}
