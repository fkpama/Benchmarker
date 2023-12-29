using Newtonsoft.Json;

namespace Benchmarker.Storage.DevOps.Serialization;

internal sealed class ArrayModel<T>
{
    public int Count { get; set; }
    public T[] Value { get; set; } = Array.Empty<T>();
}

internal sealed class DocumentModel
{
    [JsonProperty("id")]
    public string? Name { get; set; }
    [JsonProperty("value")]
    public string? Value { get; init; }
    [JsonProperty("__etag", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling =  DefaultValueHandling.Ignore)]
    public int? Etag { get; set; }
}
