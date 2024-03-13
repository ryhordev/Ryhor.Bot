﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ryhor.Bot.Helpers;
using Ryhor.Bot.Services.Interfaces;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Ryhor.Bot.Services.Implementations
{
    public class CommandService : ICommandService
    {
        private readonly ILogger<CommandService> _logger;
        private readonly IConfiguration _config;
        private readonly Dictionary<BotCommand, Func<Message, ITelegramBotClient, CancellationToken, Task>> _botCommands = [];

        public CommandService(ILoggerFactory loggerFactory, IConfiguration config)
        {
            _logger = loggerFactory.CreateLogger<CommandService>();
            _config = config;

            _botCommands.Add(new BotCommand() { Command = "/start", Description = "Start communicate with a bot" }, StartComamndHandler);
        }

        public Dictionary<BotCommand, Func<Message, ITelegramBotClient, CancellationToken, Task>> GetCommands()
        {
            return _botCommands;
        }

        private async Task StartComamndHandler(Message message, ITelegramBotClient botClient, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            Console.WriteLine($"Received a '{message.Text}' message in chat: {chatId}.");

            var sb = new StringBuilder();
            sb.AppendLine($"Hi, {message.Chat.FirstName}!");

            if (message.Chat.Username == _config[EnvironmentVariables.WIFE_NICK])
                sb.AppendLine(Answers.Greeting.WIFE);
            else
                sb.AppendLine(Answers.Greeting.OTHER);

            sb.AppendLine(Answers.Greeting.INTRODUCTION);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"{sb}",
                cancellationToken: cancellationToken);
        }

    }
}
