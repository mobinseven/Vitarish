using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.Communications.Contracts.Messages;
using Vitarish.Communications.Core.Messages.Application.Commands;
using Vitarish.Communications.Core.Messages.Application.Queries;
using Vitarish.Communications.Core.Messages.Domain.Models;

namespace Vitarish.Communications.Api.Messages.Endpoints;

public static class MessagesEndpoints
{
    public static void MapMessagesEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/communications/messages")
                          .WithTags("Messages")
                          .RequireAuthorization(new AuthorizeAttribute
                          {
                              AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
                          });

        // --------------------------- POST /api/v1/communications/messages/sms ---------------------------
        group.MapPost("/sms",
            async ([FromBody] MessageDto message, [FromServices] IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new SendSms(message.Recipient, message.Body);
                var result = await mediator.Send(command, cancellationToken);
                
                if (result.IsFailure)
                {
                    return Results.BadRequest(result.Error);
                }
                
                return Results.Ok(MapToDto(result.Value));
            })
             .WithName("SendSms")
             .Accepts<MessageDto>("application/json")
             .Produces<SmsSendResultDto>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .WithSummary("Send an SMS message")
             .WithDescription("Sends a plain SMS message to the recipient specified in the request body.");

        // --------------------------- GET /api/v1/communications/messages/sms/{messageId} ---------------------------
        group.MapGet("/sms/{messageId}",
            async ([FromRoute] string messageId, [FromServices] IMediator mediator, CancellationToken cancellationToken) =>
            {
                if (!long.TryParse(messageId, out long messageIdLong))
                {
                    return Results.BadRequest(new { error = "Invalid message ID format. Message ID must be a numeric value." });
                }
                
                var query = new GetSmsDeliveryState(messageIdLong);
                var result = await mediator.Send(query, cancellationToken);
                
                if (result is null)
                {
                    return Results.NotFound();
                }
                
                return Results.Ok(MapToDto(result));
            })
             .WithName("GetSmsDeliveryState")
             .Produces<SmsDeliveryStateDto>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .WithSummary("Get SMS delivery status")
             .WithDescription("Retrieves delivery information for a previously sent SMS. The messageId should be the value returned from the SendSms endpoint.");

        // --------------------------- POST /api/v1/communications/messages/smscode ---------------------------
        group.MapPost("/smscode",
            async ([FromBody] MessageDto message, [FromServices] IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new SendSmsCode(message.Recipient, message.Body);
                var result = await mediator.Send(command, cancellationToken);
                
                if (result.IsFailure)
                {
                    return Results.BadRequest(result.Error);
                }
                
                return Results.Ok();
            })
             .WithName("SendSmsCode")
             .Accepts<MessageDto>("application/json")
             .Produces(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .WithSummary("Send an SMS verification code")
             .WithDescription("Sends an SMS containing a verification code to the recipient.");

        // --------------------------- POST /api/v1/communications/messages/email ---------------------------
        group.MapPost("/email",
            async ([FromBody] MessageDto message, [FromServices] IMediator mediator, CancellationToken cancellationToken) =>
            {
                var command = new SendEmail(message.Recipient, message.Subject, message.Body, message.HtmlBody);
                var result = await mediator.Send(command, cancellationToken);
                
                if (result.IsFailure)
                {
                    return Results.BadRequest(result.Error);
                }
                
                return Results.Ok();
            })
             .WithName("SendEmail")
             .Accepts<MessageDto>("application/json")
             .Produces(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .WithSummary("Send an email message")
             .WithDescription("Sends an email to the recipient specified in the request body. Supports both plain text body and HTML body.");
    }

    private static SmsSendResultDto MapToDto(SmsSendResult model)
    {
        return new SmsSendResultDto(
            MessageId: model.MessageId,
            Recipient: model.Recipient,
            Cost: model.Cost,
            PackId: model.PackId,
            SubmittedAt: model.SubmittedAt,
            RelatedMessageIds: model.RelatedMessageIds,
            Metadata: model.Metadata);
    }

    private static SmsDeliveryStateDto MapToDto(SmsDeliveryState model)
    {
        return new SmsDeliveryStateDto(
            MessageId: model.MessageId,
            Recipient: model.Recipient,
            StatusCode: model.StatusCode,
            Status: model.Status,
            MessageText: model.MessageText,
            Cost: model.Cost,
            SentAt: model.SentAt,
            DeliveredAt: model.DeliveredAt,
            Metadata: model.Metadata);
    }
}
