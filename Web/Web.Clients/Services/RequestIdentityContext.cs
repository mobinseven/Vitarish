using Vitarish.Web.Clients.Abstractions;
using Vitarish.Web.Clients.Contracts;

namespace Vitarish.Web.Clients.Services;

/// <summary>
/// Default implementation of request identity context.
/// </summary>
internal sealed class RequestIdentityContext : IRequestIdentityContext
{
    /// <inheritdoc />
    public string Authority { get; private set; } = string.Empty;

    /// <inheritdoc />
    public string ClientId { get; private set; } = string.Empty;

    /// <inheritdoc />
    public string OwnerId { get; private set; } = string.Empty;

    /// <inheritdoc />
    public string? UserId { get; private set; }

    /// <inheritdoc />
    public string? TenantId { get; private set; }

    /// <inheritdoc />
    public string? ResourceId { get; private set; }

    /// <inheritdoc />
    public string? TraceId { get; private set; }

    /// <inheritdoc />
    public string? RequestId { get; private set; }

    /// <inheritdoc />
    public string? AdditionalInfo { get; private set; }

    /// <summary>
    /// Writes context from a command.
    /// </summary>
    public void Write(RequestIdentityContextCommand command)
    {
        Authority = command.Authority;
        ClientId = command.ClientId;
        OwnerId = command.OwnerId;
        TenantId = command.TenantId;
        UserId = command.UserId;
        ResourceId = command.ResourceId;
        TraceId = command.TraceId;
        RequestId = command.RequestId;
        AdditionalInfo = command.AdditionalInfo;
    }

    /// <summary>
    /// Writes context from individual values.
    /// </summary>
    public void Write(string authority, string clientId, string ownerId, string? userId = null)
    {
        Authority = authority;
        ClientId = clientId;
        OwnerId = ownerId;
        UserId = userId;
    }
}
