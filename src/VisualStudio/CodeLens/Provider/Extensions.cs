using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Text;

namespace Benchmarker.VisualStudio.CodeLens
{
    internal static class Extensions
    {
        internal static TextSpan ToTextSpan(this Span span)
            => new(span.Start, span.Length);
        internal static bool TryGetRoslyInfos(
            this CodeLensDescriptorContext context,
            CodeLensDescriptor descriptor,
            [NotNullWhen(true)]out RoslyRequestData? request)
        {
            if (context.ApplicableSpan is null
                || context.Properties is null)
            {
                request = default;
                return false;
            }

            Guid projectId, documentId;
            string? managedType, managedMethod;
            if (!context.Properties.TryGetValue(DescriptorContextPropertyNames.RoslynProjectIdGuid,
                out object propertyValue)
                || !toGuid(propertyValue, out projectId)
                || !context.Properties.TryGetValue(DescriptorContextPropertyNames.RoslynDocumentIdGuid,
                out propertyValue)
                || !toGuid(propertyValue, out documentId))
            {
                request = default;
                return false;
            }

            // succeeded. The rest is optional
            context.Properties
                .TryGetValue(DescriptorContextPropertyNames.RoslynProjectIdGuid,
                            out propertyValue);
            managedType = propertyValue as string;
            context.Properties.TryGetValue(DescriptorContextPropertyNames.RoslynProjectIdGuid,
                            out propertyValue);
            managedMethod = propertyValue as string;

            request = new(projectId,
                descriptor.ProjectGuid,
                documentId,
                context.ApplicableSpan.Value.ToTextSpan(),
                descriptor.FilePath,
                managedType,
                managedMethod);
            return true;

            static bool toGuid(object value, [NotNullWhen(true)]out Guid guid)
            {
                if (value is Guid guid1)
                {
                    guid = guid1;
                    return true;
                }
                return Guid.TryParse(value.ToString(), out guid);
            }
        }
    }
}
