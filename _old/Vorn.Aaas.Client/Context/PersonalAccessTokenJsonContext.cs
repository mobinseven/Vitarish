namespace Vorn.Aaas.Client.Context;

using System.Text.Json.Serialization;
using Vorn.Aaas.Client.Models;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CreatePersonalAccessTokenRequest))]
[JsonSerializable(typeof(PersonalAccessTokenResponse))]
internal sealed partial class PersonalAccessTokenJsonContext : JsonSerializerContext;
