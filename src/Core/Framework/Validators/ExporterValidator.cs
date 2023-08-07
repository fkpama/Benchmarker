using BenchmarkDotNet.Validators;
using Benchmarker.Framework.Exporters;

namespace Benchmarker.Framework.Validators
{
    internal class ExporterValidator : IValidator
    {
        private static WeakReference<ExporterValidator>? s_Instance;
        internal static ExporterValidator Instance
        {
            [DebuggerStepThrough]
            get => SW.GetTarget(ref s_Instance);
        }

        public bool TreatsWarningsAsErrors { get; }

        public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
        {
            var ar = validationParameters.Config.GetExporters()
                .OfType<BenchmarkerExporter>()
                .Distinct()
                .ToArray();
            if (ar.Length > 1)
            {
                for(var i = 1; i < ar.Length; i++)
                {
                    ar[i].Disabled = true;
                }
            }
            return Enumerable.Empty<ValidationError>();
        }
    }
}
