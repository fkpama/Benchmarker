namespace Benchmarker.VisualStudio
{
    public interface IBenchmarkListener
    {
        void OnProjectAdded(IBenchmarkService service, IBenchmarkProject bproj);
    }
}