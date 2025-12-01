namespace Vitarish.Web.Users.Infrastructure.Extensions;

using System.Collections.Generic;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class IdentityJsonContext : JsonSerializerContext;
