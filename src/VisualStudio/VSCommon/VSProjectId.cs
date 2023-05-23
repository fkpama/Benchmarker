using StronglyTypedIds;

namespace Benchmarker.VisualStudio
{
    [StronglyTypedId(StronglyTypedIdBackingType.Guid,
        converters: StronglyTypedIdConverter.SystemTextJson,
        implementations: StronglyTypedIdImplementations.Default)]
    public partial struct RoslynProjectId
    {
        public static implicit operator RoslynProjectId(Guid id) => new(id);
        public static implicit operator Guid(RoslynProjectId id) => id.Value;
    }
    [StronglyTypedId(StronglyTypedIdBackingType.Guid,
        converters: StronglyTypedIdConverter.SystemTextJson,
        implementations: StronglyTypedIdImplementations.Default)]
    public partial struct VSProjectId
    {
        public static implicit operator VSProjectId(Guid id) => new(id);
        public static implicit operator Guid(VSProjectId id) => id.Value;
    }
}