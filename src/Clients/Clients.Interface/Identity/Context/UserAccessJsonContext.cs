namespace Vitarish.Client.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(IEnumerable<ClaimDto>))]
[JsonSerializable(typeof(ClaimDto))]
[JsonSerializable(typeof(IEnumerable<UserDto>))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(bool))]
internal sealed partial class UserAccessJsonContext : JsonSerializerContext;
