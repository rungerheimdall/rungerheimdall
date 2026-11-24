using BenchmarkDotNet.Running;
using RungerHeimdall.Benchmarks;

BenchmarkSwitcher
    .FromAssembly(typeof(ParamsArrayBenchmarks).Assembly)
    .Run(args);