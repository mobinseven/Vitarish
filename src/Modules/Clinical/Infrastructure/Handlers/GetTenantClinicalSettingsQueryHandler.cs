using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Metadata;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Domain.Options;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetTenantClinicalSettingsQueryHandler(
    IRequestContext requestContext,
    IHttpContextAccessor httpContextAccessor,
    IPartiesService partiesService,
    IOptionsMonitor<ClinicalOptions> options) : IRequestHandler<GetTenantClinicalSettingsQuery, ClinicalTenantSettings>
{
    private readonly ClinicalOptions _options = options.CurrentValue;

    public async Task<ClinicalTenantSettings> Handle(GetTenantClinicalSettingsQuery request, CancellationToken cancellationToken)
    {
        string? tenantId = requestContext.TenantId ?? httpContextAccessor.HttpContext?.GetTenantId();

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return ClinicalTenantMetadata.Read(null, _options);
        }

        try
        {
            Vitarish.Parties.Domain.Dtos.TenantReadDto? tenant = await partiesService.GetTenantAsync(Guid.Parse(tenantId), cancellationToken);
            return ClinicalTenantMetadata.Read(tenant?.Metadata, _options);
        }
        catch
        {
            return ClinicalTenantMetadata.Read(null, _options);
        }
    }
}
