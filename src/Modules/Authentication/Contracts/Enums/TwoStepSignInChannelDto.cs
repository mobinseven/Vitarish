using System.Text.Json.Serialization;

namespace Vitarish.Authentication.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<TwoStepSignInChannelDto>))]
public enum TwoStepSignInChannelDto
{
    Phone,
    Email
}
