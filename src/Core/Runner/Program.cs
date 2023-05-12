// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using Sodiware.Benchmarker;
using Sodiware.Benchmarker.Storage;

var outputPath = @"C:\Temp\test.log";
var cancellationToken = CancellationToken.None;
var store = new JsonStorage();
var exporter = new ExportParser(store);

var run = await exporter.ParseAsync(outputPath, cancellationToken);
Debug.Assert(run is not null);

var records = run.Benchmarks[0];

var dt = await records.Benchmark
    .GetLastRunAsync(cancellationToken);

Console.WriteLine(dt);
