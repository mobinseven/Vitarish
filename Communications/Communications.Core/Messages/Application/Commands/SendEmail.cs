using MediatR;
using Vitarish.Base.Domain.Results;

namespace Vitarish.Communications.Core.Messages.Application.Commands;

/// <summary>
/// Command to send an email message.
/// </summary>
public sealed record SendEmail(
    string? Recipient,
    string? Subject,
    string? Body,
    string? HtmlBody) : IRequest<Result>;
