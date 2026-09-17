using FluentValidation;
using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Commands;

public sealed record ProvisionTenantMemberCommand(
    string? UserId,
    string? Email,
    string? PhoneNumber,
    string? FirstName,
    string? LastName) : IRequest<ProvisionTenantMemberResult>;

public sealed record ProvisionTenantMemberResult(User? User, string RoleSuffix, bool Created, bool AlreadyMember, string? ErrorCode = null);

public sealed class ProvisionTenantMemberCommandValidator : AbstractValidator<ProvisionTenantMemberCommand>
{
    public ProvisionTenantMemberCommandValidator()
    {
        _ = RuleFor(command => command).Must(command =>
            new[] { command.UserId, command.Email, command.PhoneNumber }.Count(value => !string.IsNullOrWhiteSpace(value)) == 1)
            .WithMessage("Exactly one user identifier or contact is required.");
        _ = RuleFor(command => command.Email).EmailAddress().When(command => !string.IsNullOrWhiteSpace(command.Email));
        _ = RuleFor(command => command.FirstName).MaximumLength(100);
        _ = RuleFor(command => command.LastName).MaximumLength(100);
    }
}
