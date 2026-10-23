using System.Text.Json.Serialization;

namespace Vitarish.Intelligence.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<UserMemorySourceResponse>))]
public enum UserMemorySourceResponse { Landing, Onboarding }

/// <summary>One thing Vitarish remembers about the signed-in person.</summary>
public sealed record UserMemoryItemResponse(string Id, string Text, UserMemorySourceResponse Source, DateTimeOffset RememberedAt);

public sealed record UserMemoryResponse(IReadOnlyList<UserMemoryItemResponse> Items);

public sealed record UpdateUserMemoryItemRequest(string Text);
