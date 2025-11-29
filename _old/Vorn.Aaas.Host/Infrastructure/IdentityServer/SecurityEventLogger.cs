namespace Vorn.Aaas.Host.Infrastructure.IdentityServer;

using System.Reflection;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Microsoft.Extensions.Logging;

public class SecurityEventLogger(ILogger<SecurityEventLogger> logger) : IEventSink
{
    public Task PersistAsync(Event evt)
    {
        if(evt is null)
        {
            return Task.CompletedTask;
        }

        LogLevel level = evt.EventType switch
        {
            EventTypes.Error => LogLevel.Error,
            EventTypes.Failure => LogLevel.Warning,
            _ => LogLevel.Information
        };

        object payload = new
        {
            evt.Id,
            evt.Name,
            evt.Category,
            evt.EventType,
            ClientId = GetDetail(evt, "ClientId"),
            SubjectId = GetDetail(evt, "SubjectId"),
            evt.TimeStamp,
            Error = GetError(evt),
            evt.ActivityId
        };

        logger.Log(level, "IdentityServer event {@IdentityServerEvent}", payload);
        return Task.CompletedTask;
    }

    private static string? GetError(Event evt)
    {
        foreach(string propertyName in new[] { "ErrorDescription", "Error", "ErrorMessage" })
        {
            if(GetDetail(evt, propertyName) is { Length: > 0 } value)
            {
                return value;
            }
        }

        return null;
    }

    private static string? GetDetail(Event evt, string propertyName)
        => evt.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance) is { } property
            && property.PropertyType == typeof(string)
            && property.GetValue(evt) is string value
            && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
}
