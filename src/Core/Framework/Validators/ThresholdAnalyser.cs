using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using Benchmarker.Testing;
using Benchmarker.Validation;

namespace Benchmarker.Framework.Validators
{
    internal class ThresholdAnalyser : IAnalyser
    {
        private readonly TestCaseCollection? collection;
        private readonly IBenchmarkIdGenerator idGenerator;

        public string Id { get; } = nameof(ThresholdAnalyser);

        public ThresholdAnalyser()
            : this(null, BenchmarkIdGenerator.Instance)
        { }
        public ThresholdAnalyser(TestCaseCollection? collection,
            IBenchmarkIdGenerator idGenerator)
        {
            this.collection = collection;
            this.idGenerator = idGenerator;
        }

        public IEnumerable<Conclusion> Analyse(Summary summary)
        {
            var lst = new List<Conclusion>();
            //var current = new List<Conclusion>();
            foreach (var bdnTc in summary.BenchmarksCases)
            {
                if (!summary.HasReport(bdnTc))
                {
                    continue;
                }
                var reports = summary.GetReports(bdnTc).ToArray();
                if (reports.Length == 0)
                    continue;
                var collection = this.collection
                ?? CorePlatform.GetCollection(bdnTc, summary);
                var id = this.idGenerator.GetId(bdnTc);
                if (id.IsMissing)
                    continue;
                var ti = collection[id];
                if (ti is null)
                    continue;

                BenchmarkValidationContext? context = null;
                var validators = ti.Validators ?? Enumerable.Empty<IBenchmarkValidator>();
                foreach (var report in reports)
                {
                    foreach (var validator in validators)
                    {
                        context ??= new(this.Id, ti, summary, report);
                        try
                        {
                            validator.Validate(context);
                        }
                        catch (Exception ex)
                        {
                            var msg = $"Validator {validator.Name} exception: {ex.Message}";
                            lst.Add(Conclusion
                            .CreateError(this.Id, msg, report, false));
                            //ti.RaiseValidationError(ex);
                            CorePlatform.Log.WriteLineError(ex.ToString());
                            continue;
                        }
                        var errors = context.Errors;
                        if (errors is null)
                            continue;

                        lst.AddRange(errors.Select(x => x.CreateError(this.Id)));
                    }
                }
            }

            return lst;
        }
    }
}
