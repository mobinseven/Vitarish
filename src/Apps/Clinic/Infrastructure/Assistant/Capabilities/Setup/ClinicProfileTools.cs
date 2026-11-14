using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Setup;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Domain.Profiles;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Setup;

/// <summary>
/// Prepares a change to the clinic's public profile (description, address, phones, website) for the owner's review.
/// Only the parts given change; the rest of the profile, such as working hours and photos, stays as it is. It checks
/// the change against the current profile with the profile's own rules and never saves anything itself.
/// </summary>
public sealed class PrepareUpdateClinicProfileTool(ISender sender) : IAssistantPreparationTool
{
    private const string PreparedStatus = "prepared";
    private const string RejectedStatus = "rejected";

    public AgentToolDefinition Definition { get; } = new(
        ClinicSetupAssistant.PrepareProfileToolName,
        "Prepare a change to the clinic's public profile for the owner to review and confirm: description, address, phone numbers, website. " +
        "Give only the parts to change; an address replaces the whole address and phones replace the whole list. The logo is set in clinic settings. This does not save anything.",
        """{"type":"object","properties":{"description":{"type":"string","maxLength":500},"address":{"type":"object","properties":{"province":{"type":"string"},"city":{"type":"string"},"street":{"type":"string"},"postalCode":{"type":"string"}},"additionalProperties":false},"phones":{"type":"array","items":{"type":"string"}},"website":{"type":"string"}},"additionalProperties":false}""");

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        ClinicProfileReview? review = JsonSerializer.Deserialize(call.ArgumentsJson, ClinicProfileJsonSerializerContext.Default.ClinicProfileReview);
        if (review is null || review is { Description: null, Address: null, Phones: null, Website: null })
            return Result(call, new(RejectedStatus, null, null, "Give at least one of description, address, phones or website."), isError: true);

        TenantBusinessProfileView? current = await sender.Send(new GetTenantBusinessProfileQuery(), cancellationToken);
        if (current is null)
            return Result(call, new(RejectedStatus, null, null, "The clinic was not found."), isError: true);
        if (!ClinicProfileChange.TryApply(current, review, out _, out _, out string? problem))
            return Result(call, new(RejectedStatus, null, null, problem), isError: true);

        return Result(call, new(PreparedStatus, Guid.NewGuid(), review, null));
    }

    public AssistantPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        if (result.IsError) return null;
        try
        {
            ClinicProfilePreparation? preparation = JsonSerializer.Deserialize(
                result.Output, ClinicProfileJsonSerializerContext.Default.ClinicProfilePreparation);
            return preparation is { Status: PreparedStatus, ActionId: { } actionId, Review: { } review }
                ? new(
                    ClinicSetupAssistant.CapabilityName,
                    ClinicSetupAssistant.ProfileActionType,
                    actionId,
                    JsonSerializer.Serialize(review, ClinicProfileJsonSerializerContext.Default.ClinicProfileReview),
                    null)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AgentToolResult Result(AgentToolCall call, ClinicProfilePreparation value, bool isError = false) =>
        new(call.Id, JsonSerializer.Serialize(value, ClinicProfileJsonSerializerContext.Default.ClinicProfilePreparation), isError);
}

/// <summary>Applies a reviewed change onto the current profile, keeping every part the change does not name.</summary>
public static class ClinicProfileChange
{
    public static bool TryApply(
        TenantBusinessProfileView current,
        ClinicProfileReview change,
        out string? description,
        out TenantBusinessDetails? details,
        out string? problem)
    {
        description = change.Description is null ? current.Description : change.Description.Trim();
        details = null;
        if (description?.Length > CreateOwnTenantCommandHandler.DescriptionMaximumLength)
        {
            problem = $"The description can have at most {CreateOwnTenantCommandHandler.DescriptionMaximumLength} characters.";
            return false;
        }

        TenantBusinessDetails? existing = current.Details;
        TenantBusinessAddress? address = change.Address is { } given
            ? new(given.Province, given.City, given.Street, given.PostalCode, existing?.Address?.Latitude, existing?.Address?.Longitude)
            : existing?.Address;
        details = TenantBusinessDetails.TryCreate(
            address,
            change.Phones ?? existing?.Phones,
            change.Website ?? existing?.Website,
            existing?.SocialLinks,
            existing?.WorkingHours,
            existing?.PhotoFileIds,
            out problem);
        return problem is null;
    }
}

/// <summary>What the owner confirms: only the parts of the profile that change.</summary>
public sealed record ClinicProfileReview(
    string? Description,
    ClinicProfileAddress? Address,
    IReadOnlyList<string>? Phones,
    string? Website);

public sealed record ClinicProfileAddress(string? Province, string? City, string? Street, string? PostalCode);

public sealed record ClinicProfilePreparation(string Status, Guid? ActionId, ClinicProfileReview? Review, string? Problem);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ClinicProfileReview))]
[JsonSerializable(typeof(ClinicProfilePreparation))]
internal sealed partial class ClinicProfileJsonSerializerContext : JsonSerializerContext;
