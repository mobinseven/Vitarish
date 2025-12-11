using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Application.Commands;
using Vitarish.Communications.Application.Queries;
using Vitarish.Communications.Domain.Models;

namespace Vitarish.Communications.Hosting.Presentation.Api;

public static class MessagesEndpoints
{
    public static void MapMessagesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/v1/caas/messages")
                          .WithTags("Messages");

        _ = group.MapPost(
            "/sms",
            async ([FromBody] Message message, [FromServices] IMessagesService service, CancellationToken cancellationToken) =>
            {
                SmsSendResult result = await SendSms.ExecuteAsync(service, message, cancellationToken);
                return Results.Ok(result);
            })
             .WithName("SendSms")
             .Accepts<Message>("application/json")
             .Produces<SmsSendResult>(StatusCodes.Status200OK)
             .WithSummary("Send an SMS message")
             .WithDescription("Sends a plain SMS message to the recipient specified in the request body.");

        _ = group.MapGet(
            "/sms/{messageId:long}",
            async ([FromRoute] long messageId, [FromServices] IMessagesService service, CancellationToken cancellationToken) =>
            {
                SmsDeliveryState result = await GetSmsDeliveryState.ExecuteAsync(service, messageId, cancellationToken);
                return Results.Ok(result);
            })
             .WithName("GetSmsDeliveryState")
             .Produces<SmsDeliveryState>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .WithSummary("Get SMS delivery status")
             .WithDescription("Retrieves delivery information for a previously sent SMS.");

        _ = group.MapPost(
            "/smscode",
            async ([FromBody] Message message, [FromServices] IMessagesService service, CancellationToken cancellationToken) =>
            {
                await SendSmsCode.ExecuteAsync(service, message, cancellationToken);
                return Results.Ok();
            })
             .WithName("SendSmsCode")
             .Accepts<Message>("application/json")
             .Produces(StatusCodes.Status200OK)
             .WithSummary("Send an SMS verification code")
             .WithDescription("Sends an SMS containing a verification code to the recipient.");

        _ = group.MapPost(
            "/email",
            async ([FromBody] Message message, [FromServices] IMessagesService service, CancellationToken cancellationToken) =>
            {
                await SendEmail.ExecuteAsync(service, message, cancellationToken);
                return Results.Ok();
            })
             .WithName("SendEmail")
             .Accepts<Message>("application/json")
             .Produces(StatusCodes.Status200OK)
             .WithSummary("Send an email message")
             .WithDescription("Sends an email to the recipient specified in the request body.");
    }
}
