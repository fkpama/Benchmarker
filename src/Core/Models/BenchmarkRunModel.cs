#nullable disable

namespace Benchmarker.Serialization
{
    public sealed class BenchmarkRunModel
    {
        public DateTime TimeStamp { get; set; }
        public string? Title { get; set; }
        public string? CommitId { get; set; }
        public List<BenchmarkRecord> Records { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public BenchmarkRunModel() : this(DateTime.UtcNow) { }

        public BenchmarkRunModel(DateTime utcNow)
        {
            this.TimeStamp = utcNow.ToUniversalTime();
        }

        public void Add(BenchmarkRecord record)
        {
            (this.Records ??= new()).Add(record);
        }

        internal bool TryGetRecord(Guid id,
                                   out BenchmarkRecord record)
        {
            return (record = this.Records?.Find(x => x.DetailId == id))
                is not null;
        }
    }
}
#nullable restore
