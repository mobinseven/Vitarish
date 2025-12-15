namespace Vitarish.Client.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CreatePersonalAccessTokenRequest))]
[JsonSerializable(typeof(PersonalAccessTokenResponse))]
internal sealed partial class PersonalAccessTokenJsonContext : JsonSerializerContext;
