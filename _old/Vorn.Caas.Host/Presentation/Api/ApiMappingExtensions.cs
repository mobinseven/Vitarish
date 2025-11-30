using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vorn.Aaas.Client.Api.Filters;
using Vorn.Communications.Abstractions;
using Vorn.Communications.Commands;
using Vorn.Communications.Models;
using Vorn.Communications.Queries;

public static class MessagesEndpoints
{
    public static void MapMessagesEndpoints(this WebApplication app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/caas/messages")
                          .WithTags("Messages")
                          .RequireAuthorization(new AuthorizeAttribute
                          {
                              AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme
                          })
                          .AddEndpointFilter<RequestIdentityContextEndpointFilter>();

        // --------------------------- POST /api/messages/sms ---------------------------
        _ = group.MapPost("/sms",
            async ([FromBody] Message message, [FromServices] IMessagesService service, CancellationToken cancellationToken) =>
            {
                SmsSendResult result = await SendSms.ExecuteAsync(service, message, cancellationToken);
                return Results.Ok(result);
            })
             .WithName("SendSms")
             .Accepts<Message>("application/json")
             .Produces<SmsSendResult>(StatusCodes.Status200OK)
             .WithOpenApi(op =>
             {
                 op.Summary = "Send an SMS message";
                 op.Description = "Sends a plain SMS message to the recipient specified in the request body.";
                 if(op.RequestBody is not null)
                 {
                     op.RequestBody.Description = "The message payload containing phone number, body text, etc.";
                 }
                 return op;
             });

        // --------------------------- GET /api/messages/sms/{messageId} ---------------------------
        _ = group.MapGet("/sms/{messageId:long}",
            async ([FromRoute] long messageId, [FromServices] IMessagesService service, CancellationToken cancellationToken) =>
            {
                SmsDeliveryState result = await GetSmsDeliveryState.ExecuteAsync(service, messageId, cancellationToken);
                return Results.Ok(result);
            })
             .WithName("GetSmsDeliveryState")
             .Produces<SmsDeliveryState>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status400BadRequest)
             .WithOpenApi(op =>
             {
                 op.Summary = "Get SMS delivery status";
                 op.Description = "Retrieves delivery information for a previously sent SMS.";
                 return op;
             });

        // --------------------------- POST /api/messages/smscode ---------------------------
        _ = group.MapPost("/smscode",
            async ([FromBody] Message message, [FromServices] IMessagesService service, CancellationToken cancellationToken) =>
            {
                await SendSmsCode.ExecuteAsync(service, message, cancellationToken);
                return Results.Ok();
            })
             .WithName("SendSmsCode")
             .Accepts<Message>("application/json")
             .Produces(StatusCodes.Status200OK)
             .WithOpenApi(op =>
             {
                 op.Summary = "Send an SMS verification code";
                 op.Description = "Sends an SMS containing a verification code to the recipient.";
                 if(op.RequestBody is not null)
                 {
                     op.RequestBody.Description = "The message payload containing phone number and code.";
                 }
                 return op;
             });

        // --------------------------- POST /api/messages/email ---------------------------
        _ = group.MapPost("/email",
            async ([FromBody] Message message, [FromServices] IMessagesService service, CancellationToken cancellationToken) =>
            {
                await SendEmail.ExecuteAsync(service, message, cancellationToken);
                return Results.Ok();
            })
             .WithName("SendEmail")
             .Accepts<Message>("application/json")
             .Produces(StatusCodes.Status200OK)
             .WithOpenApi(op =>
             {
                 op.Summary = "Send an email message";
                 op.Description = "Sends an email to the recipient specified in the request body.";
                 if(op.RequestBody is not null)
                 {
                     op.RequestBody.Description = "The message payload containing email address, subject, and body.";
                 }
                 return op;
             });
    }
}
