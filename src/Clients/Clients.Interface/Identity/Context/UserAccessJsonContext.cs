using System.Text.Json.Serialization;
using Vitarish.Clients.Interface.Identity.Contracts;

namespace Vitarish.Client.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(IEnumerable<ClaimDto>))]
[JsonSerializable(typeof(ClaimDto))]
[JsonSerializable(typeof(IEnumerable<UserDto>))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(SetUserAccessRequest))]
[JsonSerializable(typeof(RemoveUserAccessRequest))]
internal sealed partial class UserAccessJsonContext : JsonSerializerContext;
