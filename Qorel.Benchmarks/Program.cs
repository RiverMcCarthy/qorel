using BenchmarkDotNet.Running;

// Run with: dotnet run -c Release
// Run just some of them: dotnet run -c Release --filter "*Filter*"
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
