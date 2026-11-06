using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Audit.Domain;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Users.Application.Commands;

/// <summary>
/// Changes a phone number whose destination was verified by the Authentication challenge flow.
/// This command is internal to the module boundary and is not bound from an HTTP request.
/// </summary>
public sealed record SetVerifiedUserPhoneNumberCommand(string Id, string PhoneNumber) : IRequest<IdentityResult>;

public sealed class SetVerifiedUserPhoneNumberCommandValidator : AbstractValidator<SetVerifiedUserPhoneNumberCommand>
{
    public SetVerifiedUserPhoneNumberCommandValidator(UsersManagementAudit managementAudit)
    {
        _ = RuleFor(command => command.Id).NotEmpty();
        _ = RuleFor(command => command.PhoneNumber).NotEmpty();
        _ = RuleFor(command => command.PhoneNumber)
            .Must(phoneNumber => UserPhoneNumberPolicy.TryNormalize(phoneNumber, out _))
            .WithMessage(UserPhoneNumberPolicy.InvalidPhoneNumberMessage);
        _ = RuleFor(command => command).CustomAsync(async (command, validationContext, cancellationToken) =>
        {
            bool invalid = string.IsNullOrWhiteSpace(command.Id) ||
                !UserPhoneNumberPolicy.TryNormalize(command.PhoneNumber, out _);
            if (!invalid)
                return;

            string? userId = string.IsNullOrWhiteSpace(command.Id) ? null : command.Id;
            await managementAudit.AccountEditFailedAsync(userId, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
            if (userId is not null)
                await managementAudit.SecurityAttributeChangedAsync(userId, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);
        });
    }
}
