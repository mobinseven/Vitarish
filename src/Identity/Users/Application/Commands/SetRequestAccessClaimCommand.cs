using FluentValidation;
using MediatR;
using Vitarish.Requests.Domain.Models;

namespace Vitarish.Users.Application.Commands;

/// <summary>
/// Command to set access claim for a request based on the full request context.
/// This replaces the client-centric SetUserAccessClaimCommand.
/// </summary>
public record SetRequestAccessClaimCommand(RequestIdentityDto Request, string AccessValue) : IRequest;

public class SetRequestAccessClaimCommandValidator : AbstractValidator<SetRequestAccessClaimCommand>
{
    public SetRequestAccessClaimCommandValidator()
    {
        _ = RuleFor(c => c.Request).NotNull();

        When(c => c.Request != null, () =>
        {
            _ = RuleFor(c => c.Request!.UserId).NotNull().NotEmpty();
            _ = RuleFor(c => c.Request!.ClientId).NotNull().NotEmpty();
        });
        _ = RuleFor(c => c.AccessValue).NotEmpty();
    }
}
