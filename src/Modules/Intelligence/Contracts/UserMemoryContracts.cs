using System.Text.Json.Serialization;

namespace Vitarish.Intelligence.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<UserMemorySourceResponse>))]
public enum UserMemorySourceResponse { Landing, Onboarding }

/// <summary>One thing Vitarish remembers about the signed-in person.</summary>
public sealed record UserMemoryItemResponse(string Id, string Text, UserMemorySourceResponse Source, DateTimeOffset RememberedAt);

/// <param name="MaxTextLength">The longest text a remembered fact may have, so a client can say so before saving.</param>
public sealed record UserMemoryResponse(IReadOnlyList<UserMemoryItemResponse> Items, int MaxTextLength);

public sealed record UpdateUserMemoryItemRequest(string Text);
