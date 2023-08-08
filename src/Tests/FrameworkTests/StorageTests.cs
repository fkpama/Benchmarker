using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Benchmarker.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
