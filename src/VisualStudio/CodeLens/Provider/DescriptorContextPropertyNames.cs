using Microsoft.VisualStudio.Language.CodeLens;

namespace Benchmarker.VisualStudio.CodeLens
{
    /// <summary>
    /// Some (apparently undocumented) properties that
    /// are passed in the <see cref="CodeLensDescriptorContext.Properties"/>
    /// </summary>
    internal static class DescriptorContextPropertyNames
    {
        internal const string RoslynProjectIdGuid = nameof(RoslynProjectIdGuid);
        internal const string RoslynDocumentIdGuid = nameof(RoslynDocumentIdGuid);
        internal const string UnitTestManagedType = nameof(UnitTestManagedType);
        internal const string UnitTestManagedMethod = nameof(UnitTestManagedMethod);
    }
}
