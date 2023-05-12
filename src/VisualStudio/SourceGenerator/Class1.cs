using Microsoft.CodeAnalysis;

namespace ClassLibrary1
{
    [Generator]
    public class Class1 : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var entryPoint = context.Compilation.GetEntryPoint(cancellationToken);
            var isValid = context.Compilation
                .ReferencedAssemblyNames
                .Any(x => string.Equals("Sodiware.Benchmarker", x.Name, StringComparison.OrdinalIgnoreCase));
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }
    }
}