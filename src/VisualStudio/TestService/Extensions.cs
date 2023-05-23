using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell.Interop;

namespace Benchmarker.VisualStudio.TestsService
{
    internal static class Extensions
    {
        internal static string GetFullName(this INamespaceOrTypeSymbol sym)
        {
            var lst = new List<string>();
            for (INamespaceOrTypeSymbol cur = sym;
                cur != null && (cur is not INamespaceSymbol ns
                || !ns.IsGlobalNamespace);
                cur = cur.ContainingNamespace)
                lst.Add(cur.Name);

            lst.Reverse();
            var result = string.Join(".", lst);
            return result;
        }

        internal static Project? GetProjectOfGuid(this VisualStudioWorkspace workspace, Guid guid)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach(var project in workspace.CurrentSolution.Projects)
            {
                var hier = workspace.GetHierarchy(project.Id);
                Debug.Assert(hier is not null);
                if (hier is not null)
                {
                    ErrorHandler.ThrowOnFailure(
                    hier.GetGuidProperty((uint)VSConstants.VSITEMID.Root,
                        (int)__VSHPROPID.VSHPROPID_ProjectIDGuid,
                        out var id));
                    if (id == guid)
                    {
                        return project;
                    }
                    Marshal.ReleaseComObject(hier);
                }
            }
            return null;
        }

        internal static bool ReferencesBenchmarkDotNet(this Compilation compilation)
        {

            var referenceBenchmarkDotnet = compilation
                .ReferencedAssemblyNames
                .Any(x => string.Equals(x.Name,
                BenchmarkConstants.BdnAssemblyName,
                StringComparison.OrdinalIgnoreCase));
            return referenceBenchmarkDotnet;
        }

        internal static bool HasBenchmarkAttribute(this IMethodSymbol methodSymbol)
        {
            return methodSymbol
                .GetAttributes()
                .Any(x => x.IsBenchmarkAttribute()) ;
        }

        internal static bool IsBenchmarkAttribute(this AttributeData attr)
        {
            if (attr.AttributeClass is null)
                return false;
            return attr.AttributeClass.Name
                .Equals(BenchmarkConstants.BenchmarkAttribute, StringComparison.Ordinal);
        }

        internal static bool IsBenchmarkDotNet(this IAssemblySymbol asm)
            => asm.Name.Equals(BenchmarkConstants.BdnAnnotationAssemblyName, StringComparison.OrdinalIgnoreCase);
    }
}
