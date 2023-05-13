#nullable disable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Perfolizer.Horology;

namespace Benchmarker.Serialization
{
    public class BenchmarkRunModel
    {
        public BenchmarkRunModel() : this(DateTime.UtcNow) { }

        public BenchmarkRunModel(DateTime utcNow)
        {
            this.TimeStamp = utcNow.ToUniversalTime();
        }

        public DateTime TimeStamp { get; set; }
        public string? Title { get; set; }
        public string? CommitId { get; set; }
        public List<BenchmarkRecord> Records { get; set; }

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

    public class BenchmarkDetail
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string RefId { get; set; }
        public string FullName { get; set; }
        public string MethodTitle { get; set; }
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

        internal TimeInterval GetMeanDelta(TimeInterval currentVal)
        {
            if (!this.Mean.HasValue || this.Mean < 0)
            {
                throw new InvalidOperationException();
            }
            var old = this.Mean.Value;
            var to = TimeInterval.FromNanoseconds(old);
            var res = TimeInterval.FromNanoseconds(to.Nanoseconds - currentVal.Nanoseconds);
            return res;
        }
    }

    public class BenchmarkHistory
    {
        public List<BenchmarkDetail> Details { get; set; }
        public List<BenchmarkRunModel> Runs { get; set; }
        public bool TryGetDetail(Guid id, [NotNullWhen(true)] out BenchmarkDetail? run)
        {
            run = this.Details?.Find(x => x.Id == id);
            return run is not null;
        }
        public bool TryGetLastRecord(TestId id,
            [NotNullWhen(true)] out BenchmarkRecord? record)
            => TryGetLastRecord(id, out record, out _, out _);
        public bool TryGetLastRecord(TestId id,
            [NotNullWhen(true)] out BenchmarkRecord? record,
            [NotNullWhen(true)] out BenchmarkRunModel? run,
            [NotNullWhen(true)] out BenchmarkDetail? detail)
        {
            record = null;
            detail = null;
            run = null;
            if (this.TryGetDetail(id, out detail))
            {
                run = this.Runs?
                    .Where(x => x.TryGetRecord(id, out _))
                    .OrderByDescending(x => x.TimeStamp)
                    .FirstOrDefault();
                if (run is not null)
                {
                    return run.TryGetRecord(id, out record);
                }
            }
            return record is not null;
        }
        public bool TryGetLastRun(TestId id,
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

        public void Add(BenchmarkDetail detail)
            => (this.Details ??= new()).Add(detail);

        public void Add(BenchmarkRunModel run)
            => (this.Runs ??= new()).Add(run);

        public bool Remove(BenchmarkDetail old)
            => this.Details?.Remove(old) ?? false;
    }
}
#nullable restore
