#nullable disable

using System.Diagnostics.CodeAnalysis;

namespace Sodiware.Benchmarker.Serialization
{
    public class BenchmarkRunModel
    {
        public BenchmarkRunModel() : this(DateTime.UtcNow) { }

        public BenchmarkRunModel(DateTime utcNow)
        {
            this.TimeStamp = utcNow.ToUniversalTime();
        }

        public DateTime TimeStamp { get; set; }
        public List<BenchmarkRecord> Records { get; set; }

        internal void Add(BenchmarkRecord record)
        {
            (this.Records ??= new()).Add(record);
        }
    }

    public class BenchmarkDetail
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string MethodTitle { get; set; }
    }

    public class BenchmarkRecord
    {
        public Guid DetailId { get; set; }
        public double? Mean { get; set; }
        public double? BytesAllocated { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }

    public class BenchmarkHistory
    {
        public List<BenchmarkDetail> Details { get; set; }
        public List<BenchmarkRunModel> Runs { get; set; }
        public bool TryGetDetail(Guid id, [NotNullWhen(true)]out BenchmarkDetail? run)
        {
            run = this.Details?.Find(x => x.Id == id);
            return run is not null;
        }
        public bool TryGetLastRun(Guid id,
            [NotNullWhen(true)]out BenchmarkRunModel? run,
            [NotNullWhen(true)]out BenchmarkDetail? detail)
        {
            if (this.TryGetDetail(id, out detail))
            {
                run = this.Runs?
                    .OrderByDescending(x => x.TimeStamp)
                    .FirstOrDefault(x => x.Records?
                    .Any(x => x.DetailId == id) ?? false);
            }
            else
            {
                run = null;
            }
            return run is not null;
        }

        internal void Add(BenchmarkDetail detail)
            => (this.Details ??= new()).Add(detail);

        internal void Add(BenchmarkRunModel run)
            => (this.Runs ??= new()).Add(run);

        internal bool Remove(BenchmarkDetail old)
            => this.Details?.Remove(old) ?? false;
    }
}
#nullable restore
