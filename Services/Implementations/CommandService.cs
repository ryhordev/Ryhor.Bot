﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ryhor.Bot.Entities;
using Ryhor.Bot.Helpers;
using Ryhor.Bot.Helpers.Constants;
using Ryhor.Bot.Services.Interfaces;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace Ryhor.Bot.Services.Implementations
{
    public class CommandService : ICommandService
    {
        private readonly ILogger<CommandService> _logger;
        private readonly IConfiguration _config;
        private readonly Dictionary<BotCommand, CommandDictionaryValue> _botCommands = [];

        public CommandService(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<CommandService>();
            _config = config;

            _botCommands.Add(
                new BotCommand
                {
                    Command = BotConstants.CommandRoute.START,
                    Description = BotConstants.CommandDescription.START
                },
                new CommandDictionaryValue
                {
                    CommandMethod = StartComamndHandler
                });

            _botCommands.Add(
                new BotCommand
                {
                    Command = "/benchmark",
                    Description = "Test"
                },
                new CommandDictionaryValue
                {
                    CommandMethod = BenchMarkComamndHandler,
                    AnswerMethod = BenchMarkAnswerComamndHandler
                });
        }

        public Dictionary<BotCommand, CommandDictionaryValue> GetCommands()
        {
            return _botCommands;
        }

        private async Task StartComamndHandler(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Format(Answers.Greeting.HI, message.Chat.FirstName));

            var isWife = message.Chat.Username == _config[EnvironmentVariables.WIFE_NICK];

            if (isWife)
                sb.AppendLine(Answers.Greeting.WIFE);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"{sb}",
                cancellationToken: cancellationToken);

            if (isWife)
                await botClient.SendStickerAsync(
                    chatId: message.Chat.Id,
                    sticker: InputFile.FromFileId(_config["GREETING_STICKER"] ?? ""),
                    cancellationToken: cancellationToken);
        }

        private async Task BenchMarkComamndHandler(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
               chatId: message.Chat.Id,
               text: "Please send a code to start benchmark",
               cancellationToken: cancellationToken);
        }

        private async Task BenchMarkAnswerComamndHandler(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: Answers.Common.ANSWER_GENERETING,
                                cancellationToken: cancellationToken);

            string tempDirectory = Path.Combine(Path.GetTempPath(), "RYHORBOT", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            string modifiedCode = $@"
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
    public void UserBenchmarkMethod()
    {{
        {message.Text}
    }}
}}";

            string filePath = Path.Combine(tempDirectory, "UserBenchmark.cs");
            await File.WriteAllTextAsync(filePath, modifiedCode, cancellationToken);

            // Create a new project file that includes BenchmarkDotNet as a dependency
            string projectFilePath = Path.Combine(tempDirectory, "UserBenchmark.csproj");
            string projectFileContent = $@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <OutputType>Exe</OutputType>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include=""BenchmarkDotNet"" Version=""0.13.12"" />
    </ItemGroup>
</Project>";

            await File.WriteAllTextAsync(projectFilePath, projectFileContent, cancellationToken);

            var process = new Process
            {
                StartInfo =
                    {
                        FileName = "dotnet",
                        Arguments = $"run --project {projectFilePath} --configuration Release",
                        WorkingDirectory = tempDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
            };

            process.Start();


            string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            string error = await process.StandardError.ReadToEndAsync(cancellationToken);

            process.WaitForExit();

            var match = Regex.Match(output, @"\| Method\s+\| Mean\s+\| Error\s+\| StdDev\s+\|[\s\S]*");

            if (match.Success)
                output = $"```\n{match.Value}\n```";

            Directory.Delete(tempDirectory, true);

            if (!string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(error))
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: output.ReplaceTgCharacters(),
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: cancellationToken);

            if (!string.IsNullOrWhiteSpace(error))
                await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                parseMode: ParseMode.MarkdownV2,
                text: $"Error: {error.ReplaceTgCharacters()}",
                cancellationToken: cancellationToken);
        }
    }
}

/*
 // Build the Docker image
                string dockerImageName = "benchmark-image";
                string dockerFilePath = Path.Combine(tempDirectory, "Dockerfile");
                string dockerFileContent = $@"
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./
ENTRYPOINT [""dotnet"", ""UserBenchmark.dll""]";

                await File.WriteAllTextAsync(dockerFilePath, dockerFileContent, cancellationToken);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"build -t {dockerImageName} .",
                    WorkingDirectory = tempDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                })?.WaitForExit();

                // Run the Docker container
                var process = new Process
                {
                    StartInfo =
                {
                    FileName = "docker",
                    Arguments = $"run --rm {dockerImageName}",
                    WorkingDirectory = tempDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
                };
 
 */
