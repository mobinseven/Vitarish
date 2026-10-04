using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Users.Application.Commands;

public record SetUserPhoneNumberCommand(string Id, string PhoneNumber) : IRequest<IdentityResult>;

public class SetUserPhoneNumberCommandValidator : AbstractValidator<SetUserPhoneNumberCommand>
{
    public SetUserPhoneNumberCommandValidator(UsersManagementAudit managementAudit)
    {
        _ = RuleFor(c => c.Id).NotEmpty();
        _ = RuleFor(c => c.PhoneNumber).NotEmpty();
        _ = RuleFor(c => c.PhoneNumber)
            .Must(phoneNumber => UserPhoneNumberPolicy.TryNormalize(phoneNumber, out _))
            .WithMessage(UserPhoneNumberPolicy.InvalidPhoneNumberMessage);
        _ = RuleFor(c => c).CustomAsync(async (command, _, cancellationToken) =>
        {
            bool invalid = string.IsNullOrWhiteSpace(command.Id) ||
                !UserPhoneNumberPolicy.TryNormalize(command.PhoneNumber, out string? normalizedPhone);
            if (!invalid)
                return;

            string? userId = string.IsNullOrWhiteSpace(command.Id) ? null : command.Id;
            await managementAudit.AccountEditFailedAsync(userId, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            if (userId is not null)
                await managementAudit.SecurityAttributeChangedAsync(userId, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
        });
    }
}
