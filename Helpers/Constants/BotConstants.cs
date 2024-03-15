namespace Ryhor.Bot.Helpers.Constants
{
    public static class BotConstants
    {
        public static class CommandRoute
        {
            public const string START = "/start";
            public const string BENCHMARK = "/benchmark";
        }

        public static class CommandDescription
        {
            public const string START = "Begin a conversation with a bot";
            public const string BENCHMARK = "Conduct benchmark testing on your code";
        }

        public static class Benchmark
        {
            public const int TIME_FOR_EXECUTION = 2;
            public const string NO_LOGGER = "No loggers defined, you will not see any progress!";
            public const string FOLDER = "RYHORBOT";
            public const string PROJECT_NAME = "UserBenchmark.csproj";
            public const string PROJECT_BODY = $@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include=""BenchmarkDotNet"" Version=""0.13.12"" />
    </ItemGroup>
</Project>";
            public const string PROGRAM_CLASS_NAME = "Program.cs";
            public const string PROGRAM_CLASS_BODY = @"
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using System;
using System.Linq;

public class Program
{{
    public static void Main(string[] args)
    {{
        var config = new ManualConfig();
        config.AddColumnProvider(DefaultConfig.Instance.GetColumnProviders().ToArray());
        config.AddExporter(DefaultConfig.Instance.GetExporters().ToArray());
        config.AddDiagnoser(DefaultConfig.Instance.GetDiagnosers().ToArray());
        config.AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray());
        config.AddJob(DefaultConfig.Instance.GetJobs().ToArray());
        config.AddValidator(DefaultConfig.Instance.GetValidators().ToArray());
        config.UnionRule = ConfigUnionRule.AlwaysUseGlobal; // Overriding the default

        var summary = BenchmarkRunner.Run<Program>(config);
        var logger = ConsoleLogger.Default;
        MarkdownExporter.Console.ExportToLog(summary, logger);
        ConclusionHelper.Print(logger, config.GetAnalysers().FirstOrDefault()?.Analyse(summary).ToList());
    }}

    [Benchmark]
    public void YourMethod()
    {{
        {0}
    }}
}}";
        }
    }
}
