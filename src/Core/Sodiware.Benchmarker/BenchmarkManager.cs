using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Sodiware.Benchmarker.Storage;

namespace Sodiware.Benchmarker
{
    internal class BenchmarkManager
    {
        private readonly IBenchmarkStore store;
        private readonly IBenchmarkIdGenerator idGenerator;

        public BenchmarkManager(IBenchmarkStore store,
                                IBenchmarkIdGenerator idGenerator)
        {
            this.store = store;
            this.idGenerator = idGenerator;
        }
    }
}
