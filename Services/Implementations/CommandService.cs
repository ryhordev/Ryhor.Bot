using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ryhor.Bot.Entities;
using Ryhor.Bot.Helpers;
using Ryhor.Bot.Helpers.Constants;
using Ryhor.Bot.Services.Interfaces;
using System.Diagnostics;
using System.Text;
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
                    Command = BotConstants.CommandRoute.BENCHMARK,
                    Description = BotConstants.CommandDescription.BENCHMARK
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
               text: Answers.Benchmark.ENTER_CODE,
               cancellationToken: cancellationToken);
        }
        private async Task BenchMarkAnswerComamndHandler(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(
                                chatId: message.Chat.Id,
                                text: Answers.Common.ANSWER_GENERETING,
                                cancellationToken: cancellationToken);

            string tempDirectory = Path.Combine(Path.GetTempPath(), BotConstants.Benchmark.FOLDER, Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            string modifiedCode = string.Format(BotConstants.Benchmark.PROGRAM_CLASS_BODY, message.Text);

            string filePath = Path.Combine(tempDirectory, BotConstants.Benchmark.PROGRAM_CLASS_NAME);
            await File.WriteAllTextAsync(filePath, modifiedCode, cancellationToken);

            string projectFilePath = Path.Combine(tempDirectory, BotConstants.Benchmark.PROJECT_NAME);

            await File.WriteAllTextAsync(projectFilePath, BotConstants.Benchmark.PROJECT_BODY, cancellationToken);

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

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(BotConstants.Benchmark.TIME_FOR_EXECUTION));

            var processTask = process.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(-1, cancellationTokenSource.Token);

            var completedTask = await Task.WhenAny(processTask, timeoutTask);

            if (completedTask == processTask)
            {
                string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                string error = await process.StandardError.ReadToEndAsync(cancellationToken);

                process.WaitForExit();

                output = output.Replace(BotConstants.Benchmark.NO_LOGGER, "").Trim();

                Directory.Delete(tempDirectory, true);

                if (!string.IsNullOrWhiteSpace(output) && string.IsNullOrWhiteSpace(error))
                    await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"```\n{output.ReplaceTgCharacters()}\n```",
                        replyToMessageId: message.MessageId,
                        parseMode: ParseMode.MarkdownV2,
                        cancellationToken: cancellationToken);

                if (!string.IsNullOrWhiteSpace(error))
                    await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    replyToMessageId: message.MessageId,
                    parseMode: ParseMode.MarkdownV2,
                    text: $"Error: {error.ReplaceTgCharacters()}",
                    cancellationToken: cancellationToken);
            }
            else
            {
                process.Kill();
                Directory.Delete(tempDirectory, true);

                await botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: string.Format(Answers.Benchmark.STOPPED_PROCESS, BotConstants.Benchmark.TIME_FOR_EXECUTION),
                        replyToMessageId: message.MessageId,
                        cancellationToken: cancellationToken);
            }
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
