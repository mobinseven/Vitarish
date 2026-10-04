using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Security;

namespace Vitarish.Users.Application.Commands;

public record SetUserEmailCommand(string Id, string Email) : IRequest<IdentityResult>;

public class SetUserEmailCommandValidator : AbstractValidator<SetUserEmailCommand>
{
    public SetUserEmailCommandValidator(UsersManagementAudit managementAudit)
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.Email).NotEmpty().EmailAddress();
        _ = RuleFor(c => c.Email).MaximumLength(UserContactIdentifierPolicy.MaximumContactLength);
        _ = RuleFor(c => c).CustomAsync(async (command, _, cancellationToken) =>
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
