
using System.Text.Json.Serialization;
using Vorn.Aaas.Client.Contracts;

namespace Vorn.Aaas.Client.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CreatePersonalAccessTokenRequest))]
[JsonSerializable(typeof(PersonalAccessTokenResponse))]
internal sealed partial class PersonalAccessTokenJsonContext : JsonSerializerContext;
