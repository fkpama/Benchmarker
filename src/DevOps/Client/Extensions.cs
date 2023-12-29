using System.Net.Http;
using Newtonsoft.Json;

namespace Benchmarker.Storage.DevOps
{
    internal static class Extensions
    {
        internal static async Task<T?> ReadResponseAs<T>(this HttpResponseMessage message, CancellationToken cancellationToken)
            where T : class
        {
            var str = await message.Content
                .ReadAsStringAsync()
                .WithCancellation(cancellationToken)
                .NoAwait();
            if (str.IsMissing()) return null;
            return JsonConvert.DeserializeObject<T>(str);
        }
    }
}
