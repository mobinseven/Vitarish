using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Staff;

/// <summary>
/// What the owner reviews and what the commit sends: the full contact, exactly as it will be provisioned.
/// The member role is never part of it; the Users command assigns the staff role.
/// </summary>
public sealed record StaffInvitationReview(string? PhoneNumber, string? Email, string FirstName, string LastName);

/// <summary>What a confirmed invitation completes with.</summary>
public sealed record StaffInvitationCompletion(string UserId, bool Created, bool AlreadyMember);

/// <summary>
/// What the model sees after preparing an invitation: the contact is masked, and the full review is sealed with
/// Data Protection so only the server can read it when it collects the prepared action.
/// </summary>
public sealed record StaffInvitationPreparation(string Status, Guid? ActionId, string? ContactKind, string? MaskedContact, string? Message, string? SealedReview = null);

/// <summary>The sealed content of a prepared invitation: its action id and the full review.</summary>
public sealed record SealedStaffInvitation(Guid ActionId, StaffInvitationReview Review);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(StaffInvitationReview))]
[JsonSerializable(typeof(StaffInvitationCompletion))]
[JsonSerializable(typeof(StaffInvitationPreparation))]
[JsonSerializable(typeof(SealedStaffInvitation))]
internal sealed partial class StaffInvitationJsonSerializerContext : JsonSerializerContext;
