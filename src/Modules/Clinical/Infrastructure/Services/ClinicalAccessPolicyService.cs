using Microsoft.Extensions.Options;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Domain.Options;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class ClinicalAccessPolicyService(
    IOptionsMonitor<ClinicalOptions> options) : IClinicalAccessPolicyService
{
    private readonly ClinicalOptions _options = options.CurrentValue;

    public Task<ClinicalAccessPolicy> GetPolicyAsync(CancellationToken cancellationToken = default)
    {
        bool allowWrites = _options.AllowedOperations.Any(op =>
            string.Equals(op, nameof(FhirInteractionType.Create), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(op, nameof(FhirInteractionType.Update), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(op, nameof(FhirInteractionType.Patch), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(op, nameof(FhirInteractionType.Delete), StringComparison.OrdinalIgnoreCase));

        IReadOnlyList<string> allowedResourceTypes = _options.AllowedResourceTypes
            .Where(rt => !string.IsNullOrWhiteSpace(rt))
            .Select(rt => rt.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ClinicalAccessPolicy policy = new()
        {
            Enabled = _options.Enabled,
            AllowReads = true,
            AllowWrites = allowWrites,
            RequireAccessReason = _options.DefaultRequireAccessReason,
            AllowedResourceTypes = allowedResourceTypes,
            AllowedRoles = []
        };

        return Task.FromResult(policy);
    }

    public async Task<bool> CanExecuteAsync(FhirOperationRequest request, CancellationToken cancellationToken = default)
    {
        ClinicalAccessPolicy policy = await GetPolicyAsync(cancellationToken);

        if (!policy.Enabled)
        {
            return false;
        }

        bool isWrite = request.Interaction is FhirInteractionType.Create
            or FhirInteractionType.Update
            or FhirInteractionType.Patch
            or FhirInteractionType.Delete;

        if (isWrite && !policy.AllowWrites)
        {
            return false;
        }

        if (policy.AllowedResourceTypes.Count > 0 &&
            !policy.AllowedResourceTypes.Any(rt => string.Equals(rt, request.ResourceType, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (policy.RequireAccessReason && string.IsNullOrWhiteSpace(request.AccessReason))
        {
            return false;
        }

        return true;
    }
}
