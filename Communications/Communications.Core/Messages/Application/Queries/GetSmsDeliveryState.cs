using FluentValidation;
using MediatR;
using Vitarish.Communications.Core.Messages.Domain.Models;

namespace Vitarish.Communications.Core.Messages.Application.Queries;

/// <summary>
/// Query to get the delivery state of an SMS message.
/// </summary>
public sealed record GetSmsDeliveryState(long MessageId) : IRequest<SmsDeliveryState?>;

/// <summary>
/// Validator for <see cref="GetSmsDeliveryState"/>.
/// </summary>
public sealed class GetSmsDeliveryStateValidator : AbstractValidator<GetSmsDeliveryState>
{
    public GetSmsDeliveryStateValidator()
    {
        _ = RuleFor(x => x.MessageId)
            .GreaterThan(0)
            .WithMessage("Message ID must be a positive value.");
    }
}
