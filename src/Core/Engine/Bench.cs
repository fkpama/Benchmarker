using System.Collections.Immutable;
using System.Reflection;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Validators;
using Benchmarker.Engine;

namespace Benchmarker
{
    public static class BenchEngine
    {
        public static IConfig InitConfig(IConfig config, TestCaseCollection collection)
        {
            //config.AddValidator(new EventValidatorHook(collection));
            Apply(config, collection);
            return config;
        }

        class EventValidatorHook : IValidator
        {
            private readonly TestCaseCollection collection;

            public bool TreatsWarningsAsErrors { get; }

            public EventValidatorHook(TestCaseCollection collection)
            {
                this.collection = collection;
            }

            public IEnumerable<ValidationError> Validate(ValidationParameters validationParameters)
            {
                Apply(validationParameters.Config, collection);
                //foreach (var bench in validationParameters.Benchmarks)
                //{
                //    var config = bench.Config;
                //}
                return Enumerable.Empty<ValidationError>();
            }
        }

        internal static void Apply(IConfig config, TestCaseCollection collection)
        {
            var field = config.GetType()
                        .GetField("analysers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field is null)
                throw new NotImplementedException();

            var orig = (ICollection<IAnalyser>?)field.GetValue(config);
            if (orig is null)
                throw new NotImplementedException();

            var eventBaseAnalyzer = new EventAnalyzer(orig, collection);
            var analysers = new List<IAnalyser>
            {
                eventBaseAnalyzer
            };
            object fival = analysers;
            if (field.FieldType.GetGenericTypeDefinition()
                == typeof(ImmutableHashSet<>))
            {

                fival = analysers.ToImmutableHashSet();
            }
            field.SetValue(config, fival);

        }
    }
}
