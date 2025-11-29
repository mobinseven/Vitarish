namespace Vorn.Aaas.Users.Application.Commands;

using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Users.Application.Extensions;
using Vorn.Aaas.Users.Domain.Entities;

public record CreateUserWithPhoneCommand(string PhoneNumber, string? FirstName, string? LastName) : IRequest<User?>;

public class CreateUserWithPhoneCommandValidator : AbstractValidator<CreateUserWithPhoneCommand>
{
    public CreateUserWithPhoneCommandValidator()
    {
        RuleFor(c => c.PhoneNumber).NotEmpty();
    }
}

internal class CreateUserWithPhoneCommandHandler(UserManager<User> userManager) : IRequestHandler<CreateUserWithPhoneCommand, User?>
{
    public async Task<User?> Handle(CreateUserWithPhoneCommand request, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);
        return user is not null
            ? user
            : await userManager.CreateUserWithoutPasswordAsync(
                request.PhoneNumber,
                request.FirstName,
                request.LastName,
                u => userManager.SetPhoneNumberAsync(u, request.PhoneNumber));
    }
}
