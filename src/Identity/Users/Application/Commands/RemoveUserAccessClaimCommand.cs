using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Commands;

public record RemoveUserAccessClaimCommand(string UserId, string ClientId) : IRequest;

public class RemoveUserAccessClaimCommandValidator : AbstractValidator<RemoveUserAccessClaimCommand>
{
    public RemoveUserAccessClaimCommandValidator()
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
        _ = RuleFor(c => c.ClientId).NotEmpty();
    }
}
