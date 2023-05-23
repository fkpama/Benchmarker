using Microsoft.CodeAnalysis;

namespace ClassLibrary1
{
    [Generator]
    public class EntryPointGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var isValid = context
                .Compilation
                .ReferencedAssemblyNames
                .Any(x => string.Equals("Sodiware.Benchmarker", x.Name, StringComparison.OrdinalIgnoreCase));
            if (!isValid )
            {
                return;
            }
            var entryPoint = context
                .Compilation
                .GetEntryPoint(cancellationToken);
            if (entryPoint is not null)
            {
                return;
            }

            context.AddSource("GenratedProgram", );
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}