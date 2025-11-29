namespace Vorn.Aaas.Client.Context;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Vorn.Aaas.Client.Models;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(IEnumerable<ClaimDto>))]
[JsonSerializable(typeof(ClaimDto))]
[JsonSerializable(typeof(IEnumerable<UserDto>))]
[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(SetUserAccessRequest))]
[JsonSerializable(typeof(RemoveUserAccessRequest))]
internal sealed partial class UserAccessJsonContext : JsonSerializerContext;
