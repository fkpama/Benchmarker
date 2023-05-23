#nullable disable

using System.Text.Json.Serialization;

namespace Benchmarker.Serialization
{
    public class BenchmarkRecordEntry
    {
        public BenchmarkRecord Record { get; set; }
        public DateTime? DateTime { get; set; }
    }
    public class BenchmarkRecord
    {
        public TestId DetailId { get; set; }
        public double? Mean { get; set; }
        public long? BytesAllocated { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        [JsonIgnore]
        public bool HasMean
        {
            get => this.Mean.HasValue && this.Mean >= 0;
        }
    }
}
#nullable restore
