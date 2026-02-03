using System.Text.Json.Serialization;

namespace Vitarish.Interface.Infrastructure.Extensions;

[JsonSerializable(typeof(Dictionary<string, string>))]
public sealed partial class IdentityJsonContext : JsonSerializerContext;
