using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Security;

namespace Vitarish.Users.Application.Commands;

/// <summary>
/// Changes an email whose destination was verified by the Authentication challenge flow.
/// This command is internal to the module boundary and is not bound from an HTTP request.
/// </summary>
public sealed record SetVerifiedUserEmailCommand(string Id, string Email) : IRequest<IdentityResult>;

public sealed class SetVerifiedUserEmailCommandValidator : AbstractValidator<SetVerifiedUserEmailCommand>
{
    public SetVerifiedUserEmailCommandValidator(UsersManagementAudit managementAudit)
    {
        _ = RuleFor(command => command.Id).NotEmpty();
        _ = RuleFor(command => command.Email).NotEmpty().EmailAddress();
        _ = RuleFor(command => command.Email).MaximumLength(UserContactIdentifierPolicy.MaximumContactLength);
        _ = RuleFor(command => command).CustomAsync(async (command, _, cancellationToken) =>
        {
            bool invalid = string.IsNullOrWhiteSpace(command.Id) ||
                !UserContactIdentifierPolicy.IsValid(command.Email, null, null, null);
            if (!invalid)
                return;

            string? userId = string.IsNullOrWhiteSpace(command.Id) ? null : command.Id;
            await managementAudit.AccountEditFailedAsync(userId, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            if (userId is not null)
                await managementAudit.SecurityAttributeChangedAsync(userId, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
        });
    }
}
