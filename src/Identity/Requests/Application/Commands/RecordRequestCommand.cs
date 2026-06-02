
using FluentValidation;
using MediatR;

namespace Vitarish.Requests.Application.Commands;

public sealed record RecordRequestCommand(
    string RequestId,
    string Authority,
    string ClientId,
    string OwnerId,
    string? TenantId,
    string? UserId,
    string? ResourceId,
    string? TraceId,
    string? AdditionalInfo) : IRequest;

public sealed class RecordRequestIdentityCommandValidator : AbstractValidator<RecordRequestCommand>
{
    public RecordRequestIdentityCommandValidator()
    {
        _ = RuleFor(command => command.RequestId).NotEmpty();
        _ = RuleFor(command => command.Authority).NotEmpty();
        _ = RuleFor(command => command.ClientId).NotEmpty();
    }
}
