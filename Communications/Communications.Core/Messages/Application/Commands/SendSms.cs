using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Communications.Core.Messages.Domain.Models;

namespace Vitarish.Communications.Core.Messages.Application.Commands;

/// <summary>
/// Command to send an SMS message.
/// </summary>
public sealed record SendSms(string? Recipient, string? Body) : IRequest<Result<SmsSendResult>>;
