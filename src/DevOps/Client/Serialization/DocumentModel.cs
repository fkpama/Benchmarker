using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Benchmarker.Storage.DevOps.Serialization
{
    internal sealed class DocumentModel
    {
        [JsonProperty("id")]
        public string? Name { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
        [JsonProperty("__etag", NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling =  DefaultValueHandling.Ignore)]
        public int? Etag { get; set; }
    }
}
