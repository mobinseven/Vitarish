using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Contents.Contracts.Models;

namespace Vitarish.Contents.Api;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(SiteRequest))]
[JsonSerializable(typeof(ContentTypeRequest))]
[JsonSerializable(typeof(ContentSchemaRequest))]
[JsonSerializable(typeof(EntryRequest))]
[JsonSerializable(typeof(DraftRequest))]
[JsonSerializable(typeof(EntryVersionRequest))]
[JsonSerializable(typeof(RevisionActionRequest))]
[JsonSerializable(typeof(SiteResponse))]
[JsonSerializable(typeof(SiteResponse[]))]
[JsonSerializable(typeof(ContentTypeResponse))]
[JsonSerializable(typeof(ContentTypeResponse[]))]
[JsonSerializable(typeof(EntryOutcome))]
[JsonSerializable(typeof(EntryResponse))]
[JsonSerializable(typeof(EntryResponse[]))]
[JsonSerializable(typeof(RevisionResponse))]
[JsonSerializable(typeof(RevisionResponse[]))]
[JsonSerializable(typeof(PublishedEntryResponse))]
[JsonSerializable(typeof(PublishedEntryResponse[]))]
public partial class ContentsJsonSerializerContext : JsonSerializerContext;
