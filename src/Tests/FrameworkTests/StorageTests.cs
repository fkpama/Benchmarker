using Benchmarker.Serialization;

namespace FrameworkTests
{
    [TestClass]
    public class StorageTests
    {
        [TestMethod]
        public void JsonStorageSerializer__can_serialize()
        {
            var history = new BenchmarkHistory
            {
                Runs = new()
                {
                    new BenchmarkRunModel
                    {
                        Title = "Some Title"
                    }
                }
            };
        }
    }
}
