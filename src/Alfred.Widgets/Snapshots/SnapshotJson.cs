using System.Text.Json.Serialization;

namespace Alfred.Widgets.Snapshots;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SnapshotDocument))]
internal sealed partial class SnapshotJson : JsonSerializerContext;
