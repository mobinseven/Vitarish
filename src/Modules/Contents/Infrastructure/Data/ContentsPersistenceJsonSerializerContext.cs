using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Immutable;
using Vitarish.Contents.Domain.Models;

namespace Vitarish.Contents.Infrastructure.Data;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ImmutableArray<ContentFieldDefinition>), TypeInfoPropertyName = "ContentFieldDefinitions")]
[JsonSerializable(typeof(ImmutableArray<string>), TypeInfoPropertyName = "ImmutableStringArray")]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(string[]))]
internal partial class ContentsPersistenceJsonSerializerContext : JsonSerializerContext
{
}
