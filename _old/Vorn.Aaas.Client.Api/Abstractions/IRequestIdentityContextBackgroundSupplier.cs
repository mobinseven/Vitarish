namespace Vorn.Aaas.Client.Api.Abstractions;

using System;

public interface IRequestIdentityContextBackgroundSupplier
{
    IDisposable? Supply(
        string? resourceId = null,
        string? ownerId = null,
        string? tenantId = null,
        string? requestId = null,
        string? additionalInfo = null);
}
