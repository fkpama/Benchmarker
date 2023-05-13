using System.Text;
using Benchmarker.Storage;

namespace Benchmarker
{
    public interface IExportParser
    {
        Encoding Encoding { get; }
        ValueTask<Run?> ParseAsync(Stream stream, IBenchmarkStore store, CancellationToken cancellationToken);
        ValueTask<Run?> ParseAsync(string path, IBenchmarkStore store, CancellationToken cancellationToken);
    }
}