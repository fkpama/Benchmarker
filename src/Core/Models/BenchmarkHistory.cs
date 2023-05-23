#nullable disable

using System.Diagnostics.CodeAnalysis;

namespace Benchmarker.Serialization
{
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
