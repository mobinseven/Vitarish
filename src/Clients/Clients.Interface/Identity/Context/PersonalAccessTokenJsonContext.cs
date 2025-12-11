using System.Text.Json.Serialization;
using Vitarish.Clients.Interface.Identity.Contracts;

namespace Vitarish.Client.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CreatePersonalAccessTokenRequest))]
[JsonSerializable(typeof(PersonalAccessTokenResponse))]
internal sealed partial class PersonalAccessTokenJsonContext : JsonSerializerContext;
