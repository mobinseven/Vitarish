using System.Text.Json.Serialization;

namespace Vitarish.Authentication.Contracts.Responses;

public sealed record AuthenticationSessionNoticeDto(Guid Id, DateTime CreatedAt, AuthenticationSessionNoticeType NoticeType);

[JsonConverter(typeof(JsonStringEnumConverter<AuthenticationSessionNoticeType>))]
public enum AuthenticationSessionNoticeType
{
    [JsonStringEnumMemberName("parallel_session_established")]
    ParallelSessionEstablished
}
