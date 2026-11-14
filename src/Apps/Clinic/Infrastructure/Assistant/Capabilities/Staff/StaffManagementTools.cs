using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Staff;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Staff;

/// <summary>Lists the practice's members for the assistant, with masked contacts. Read-only.</summary>
public sealed class ListStaffMembersTool(ISender sender) : IAgentTool
{
    private const int MaxMembers = TenantMemberQueryDefaults.MaxResults;
    private const string SearchProperty = "search";

    public AgentToolDefinition Definition { get; } = new(
        AssistantStaffManagement.ListToolName,
        "List the practice's owners and staff members, optionally filtered by a name or contact search. Contacts are masked.",
        $$$"""{"type":"object","properties":{"{{{SearchProperty}}}":{"type":"string","maxLength":100}},"additionalProperties":false}""");

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        string? search = document.RootElement.TryGetProperty(SearchProperty, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
        IReadOnlyList<TenantMemberSummary> members = await sender.Send(new ListTenantMembersQuery(search, MaxMembers), cancellationToken);
        StaffMemberList result = new(members.Select(StaffMemberView.From).ToArray());
        return new(call.Id, JsonSerializer.Serialize(result, StaffManagementJsonSerializerContext.Default.StaffMemberList));
    }
}

/// <summary>
/// Prepares removing a staff member for the owner's review. The review names the member as the server found them,
/// and the commit removes only while that member still has the same name, so a changed review cannot remove someone
/// else. Owners cannot be removed. It never removes anyone itself.
/// </summary>
public sealed class PrepareRemoveMemberTool(ISender sender) : IAssistantPreparationTool
{
    private const string PreparedStatus = "prepared";
    private const string RejectedStatus = "rejected";
    private const string UserIdProperty = "userId";

    public AgentToolDefinition Definition { get; } = new(
        AssistantStaffManagement.PrepareRemoveToolName,
        $"Prepare removing a staff member from the practice, for the owner to review and confirm. Use a userId from {AssistantStaffManagement.ListToolName}. Owners cannot be removed. This does not remove anyone.",
        $$$"""{"type":"object","properties":{"{{{UserIdProperty}}}":{"type":"string"}},"required":["{{{UserIdProperty}}}"],"additionalProperties":false}""");

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        string userId = document.RootElement.GetProperty(UserIdProperty).GetString() ?? string.Empty;
        TenantMemberSummary? member = string.IsNullOrWhiteSpace(userId)
            ? null
            : (await sender.Send(new ListTenantMembersQuery(null, 1, [userId]), cancellationToken)).SingleOrDefault();
        if (member is null)
            return Result(call, new(RejectedStatus, null, null, "No member of this practice has that userId."), isError: true);
        StaffMemberView view = StaffMemberView.From(member);
        if (view.Role == StaffMemberView.OwnerRole)
            return Result(call, new(RejectedStatus, null, null, "Owners cannot be removed."), isError: true);

        return Result(call, new(PreparedStatus, Guid.NewGuid(), new StaffRemovalReview(view.UserId, view.Name, view.Contact), null));
    }

    public AssistantPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        if (result.IsError) return null;
        try
        {
            StaffRemovalPreparation? preparation = JsonSerializer.Deserialize(
                result.Output, StaffManagementJsonSerializerContext.Default.StaffRemovalPreparation);
            return preparation is { Status: PreparedStatus, ActionId: { } actionId, Review: { } review }
                ? new(
                    AssistantStaffInvitation.CapabilityKey,
                    AssistantStaffManagement.RemoveActionType,
                    actionId,
                    JsonSerializer.Serialize(review, StaffManagementJsonSerializerContext.Default.StaffRemovalReview),
                    null)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AgentToolResult Result(AgentToolCall call, StaffRemovalPreparation value, bool isError = false) =>
        new(call.Id, JsonSerializer.Serialize(value, StaffManagementJsonSerializerContext.Default.StaffRemovalPreparation), isError);
}

/// <summary>One practice member as the assistant and the owner's review see them.</summary>
public sealed record StaffMemberView(string UserId, string Name, string Role, string Contact)
{
    public const string OwnerRole = "owner";
    public const string StaffRole = "staff";

    public static StaffMemberView From(TenantMemberSummary member) => new(
        member.Id,
        DisplayName(member),
        member.RoleSuffix == IdentityDefinitions.TenantOwner ? OwnerRole : StaffRole,
        string.IsNullOrWhiteSpace(member.Email) && string.IsNullOrWhiteSpace(member.PhoneNumber)
            ? string.Empty
            : AssistantContactMasking.Contact(member.PhoneNumber, member.Email));

    public static string DisplayName(TenantMemberSummary member) =>
        !string.IsNullOrWhiteSpace(member.FullName) ? member.FullName.Trim()
        : string.Join(' ', new[] { member.FirstName, member.LastName }.Where(part => !string.IsNullOrWhiteSpace(part))) is { Length: > 0 } name ? name
        : member.UserName ?? member.Id;
}

public sealed record StaffMemberList(IReadOnlyList<StaffMemberView> Members);

/// <summary>What the owner confirms: the member's id and the name and masked contact the server found.</summary>
public sealed record StaffRemovalReview(string UserId, string Name, string Contact);

public sealed record StaffRemovalPreparation(string Status, Guid? ActionId, StaffRemovalReview? Review, string? Problem);

public sealed record StaffRemovalCompletion(string UserId);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(StaffMemberList))]
[JsonSerializable(typeof(StaffRemovalPreparation))]
[JsonSerializable(typeof(StaffRemovalReview))]
[JsonSerializable(typeof(StaffRemovalCompletion))]
internal sealed partial class StaffManagementJsonSerializerContext : JsonSerializerContext;
