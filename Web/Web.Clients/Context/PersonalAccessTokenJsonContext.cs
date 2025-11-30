using System.Text.Json.Serialization;
using Vitarish.Web.Clients.Contracts;

namespace Vitarish.Web.Clients.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CreatePersonalAccessTokenRequest))]
[JsonSerializable(typeof(PersonalAccessTokenResponse))]
internal sealed partial class PersonalAccessTokenJsonContext : JsonSerializerContext;
