using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Benchmarker.VisualStudio.CodeLens
{
    internal sealed class DescriptorCacheItem : IEquatable<DescriptorCacheItem>
    {
        public readonly CodeElementKinds Kind;
        public readonly Guid ProjectGuid;
        public readonly string FilePath;
        public readonly Span Span;

        public DescriptorCacheItem(CodeLensDescriptor descriptor,
            CodeLensDescriptorContext context)
            : this(descriptor.Kind, descriptor.ProjectGuid,
                  descriptor.FilePath,
                  context.ApplicableSpan ?? throw new NotImplementedException())
        { }
        public DescriptorCacheItem(CodeElementKinds kind, Guid projectGuid, string filePath, Span span)
        {
            this.Kind = kind;
            this.ProjectGuid = projectGuid;
            this.FilePath = filePath;
            this.Span = span;
        }

        public override bool Equals(object? obj)
        {
            return obj is DescriptorCacheItem item && this.Equals(item);
        }

        public bool Equals(DescriptorCacheItem other)
        {
            return this.Kind == other.Kind &&
                   this.ProjectGuid.Equals(other.ProjectGuid) &&
                   this.FilePath == other.FilePath &&
                   this.Span.Equals(other.Span);
        }

        public override int GetHashCode()
            => HashCode.Combine(this.Kind,
                                this.FilePath,
                                this.ProjectGuid,
                                this.Span);

        public static bool operator ==(DescriptorCacheItem left, DescriptorCacheItem right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DescriptorCacheItem left, DescriptorCacheItem right)
        {
            return !(left == right);
        }
    }
}
