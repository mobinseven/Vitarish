using FluentValidation;
using MediatR;

namespace Vitarish.Users.Application.Commands;

public record SetUserAccessClaimCommand(string UserId, string ClientId, string AccessValue) : IRequest;

public class SetUserAccessClaimCommandValidator : AbstractValidator<SetUserAccessClaimCommand>
{
    public SetUserAccessClaimCommandValidator()
    {
        _ = RuleFor(c => c.UserId).NotEmpty();
        _ = RuleFor(c => c.ClientId).NotEmpty();
    }
}
