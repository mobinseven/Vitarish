using System.Text.Json.Serialization;
using Vitarish.Web.Clients.Contracts;

namespace Vitarish.Web.Clients.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(IEnumerable<ClaimDto>))]
[JsonSerializable(typeof(ClaimDto))]
[JsonSerializable(typeof(IEnumerable<UserDto>))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(SetUserAccessRequest))]
[JsonSerializable(typeof(RemoveUserAccessRequest))]
internal sealed partial class UserAccessJsonContext : JsonSerializerContext;
