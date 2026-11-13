using System.Security.Cryptography;
using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Staff;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Staff;

/// <summary>
/// Prepares a staff invitation for the owner's review. It validates the contact with the rules the Users command
/// itself applies and shows the model only a masked contact. The full contact travels in the result sealed with Data
/// Protection, because the tool runs in the runtime callback request while the prepared action is collected in the
/// conversation request; only the server can open it. It never invites anyone; the member role is not an input.
/// </summary>
public sealed class PrepareInviteMemberTool(
    IDataProtectionProvider protectionProvider,
    ILogger<PrepareInviteMemberTool>? logger = null) : IAssistantPreparationTool
{
    private const string ProtectionPurpose = "Vitarish.Clinic.Assistant.StaffInvitation.Review";
    private static readonly TimeSpan ReviewLifetime = TimeSpan.FromMinutes(30);
    private readonly ITimeLimitedDataProtector _protector =
        protectionProvider.CreateProtector(ProtectionPurpose).ToTimeLimitedDataProtector();

    private const string PhoneNumberProperty = "phoneNumber";
    private const string EmailProperty = "email";
    private const string FirstNameProperty = "firstName";
    private const string LastNameProperty = "lastName";
    private const string PreparedStatus = "prepared";
    private const string RejectedStatus = "rejected";

    private static readonly HashSet<string> AllowedProperties = new(StringComparer.Ordinal)
    {
        PhoneNumberProperty, EmailProperty, FirstNameProperty, LastNameProperty
    };

    private static readonly ProvisionTenantMemberCommandValidator CommandValidator = new();

    public AgentToolDefinition Definition { get; } = new(
        AssistantStaffInvitation.PrepareToolName,
        "Prepare an invitation for a new staff member, by phone number or email (exactly one), for the owner to review " +
        "and confirm. The invited person always becomes staff; there is no role input. This does not invite anyone.",
        $$$"""
        {"type":"object","properties":{"{{{PhoneNumberProperty}}}":{"type":"string"},"{{{EmailProperty}}}":{"type":"string"},"{{{FirstNameProperty}}}":{"type":"string"},"{{{LastNameProperty}}}":{"type":"string"}},"required":["{{{FirstNameProperty}}}","{{{LastNameProperty}}}"],"additionalProperties":false}
        """);

    public Task<AgentToolResult> ExecuteAsync(
        AgentToolCall call,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (!TryBuildReview(call.ArgumentsJson, out StaffInvitationReview? review, out string? problem))
            return Task.FromResult(Result(call, new(RejectedStatus, null, null, null, problem), isError: true));

        Guid actionId = Guid.NewGuid();
        string sealedReview = _protector.Protect(
            JsonSerializer.Serialize(new SealedStaffInvitation(actionId, review), StaffInvitationJsonSerializerContext.Default.SealedStaffInvitation),
            ReviewLifetime);
        string masked = AssistantContactMasking.Contact(review.PhoneNumber, review.Email);
        if (logger is not null)
            StaffInvitationTelemetry.Prepared(logger, masked, call.Id);
        return Task.FromResult(Result(call, new(
            PreparedStatus,
            actionId,
            review.Email is null ? PhoneNumberProperty : EmailProperty,
            masked,
            "Awaiting the owner's confirmation; the invited person will be staff.",
            sealedReview)));
    }

    public AssistantPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        if (result.IsError) return null;
        try
        {
            StaffInvitationPreparation? preparation = JsonSerializer.Deserialize(
                result.Output, StaffInvitationJsonSerializerContext.Default.StaffInvitationPreparation);
            if (preparation is not { Status: PreparedStatus, ActionId: { } actionId, SealedReview: { } sealedReview })
                return null;
            SealedStaffInvitation? opened = JsonSerializer.Deserialize(
                _protector.Unprotect(sealedReview), StaffInvitationJsonSerializerContext.Default.SealedStaffInvitation);
            // The sealed id must match the visible one, so a sealed review cannot be attached to another action.
            return opened is not null && opened.ActionId == actionId
                ? new(
                    AssistantStaffInvitation.CapabilityKey,
                    AssistantStaffInvitation.ActionType,
                    actionId,
                    JsonSerializer.Serialize(opened.Review, StaffInvitationJsonSerializerContext.Default.StaffInvitationReview),
                    null)
                : null;
        }
        catch (Exception exception) when (exception is JsonException or CryptographicException or FormatException)
        {
            return null;
        }
    }

    private static bool TryBuildReview(
        string argumentsJson,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out StaffInvitationReview? review,
        out string? problem)
    {
        review = null;
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(argumentsJson);
        }
        catch (JsonException)
        {
            problem = "The arguments must be a JSON object.";
            return false;
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                root.EnumerateObject().Any(property => !AllowedProperties.Contains(property.Name)))
            {
                problem = "Only phoneNumber or email, firstName and lastName are accepted.";
                return false;
            }

            string? phoneNumber = Text(root, PhoneNumberProperty);
            string? email = Text(root, EmailProperty);
            string? firstName = Text(root, FirstNameProperty);
            string? lastName = Text(root, LastNameProperty);
            if (string.IsNullOrWhiteSpace(phoneNumber) == string.IsNullOrWhiteSpace(email))
            {
                problem = "Provide exactly one of phoneNumber or email.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                problem = "firstName and lastName are required.";
                return false;
            }

            // The same normalization and contact rules ProvisionTenantMemberCommand's handler applies.
            string? contactPhone = null;
            if (!string.IsNullOrWhiteSpace(phoneNumber) &&
                !UserPhoneNumberPolicy.TryNormalize(phoneNumber, out contactPhone))
            {
                problem = "The phone number is not a valid mobile number.";
                return false;
            }

            string? contactEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            ProvisionTenantMemberCommand command = new(null, contactEmail, contactPhone, firstName.Trim(), lastName.Trim());
            ValidationResult validation = CommandValidator.Validate(command);
            if (!validation.IsValid ||
                !UserContactIdentifierPolicy.IsValid(command.Email, command.PhoneNumber, command.FirstName, command.LastName))
            {
                problem = "The contact or name is not valid.";
                return false;
            }

            review = new(command.PhoneNumber, command.Email, command.FirstName!, command.LastName!);
            problem = null;
            return true;
        }
    }

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static AgentToolResult Result(AgentToolCall call, StaffInvitationPreparation value, bool isError = false) =>
        new(call.Id, JsonSerializer.Serialize(value, StaffInvitationJsonSerializerContext.Default.StaffInvitationPreparation), isError);
}
