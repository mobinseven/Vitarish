using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Communications.Core.Messages.Application.Commands;

/// <summary>
/// Command to send an SMS verification code.
/// </summary>
public sealed record SendSmsCode(string? Recipient, string? Code) : IRequest<Result>;
