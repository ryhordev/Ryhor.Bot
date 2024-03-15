using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ryhor.Bot.Entities;
using Ryhor.Bot.Helpers.Constants;
using Ryhor.Bot.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Ryhor.Bot.Services.Implementations
{
    public class ChatService : IChatService
    {
        private readonly ILogger<ChatService> _logger;
        private readonly IConfiguration _config;
        private readonly Dictionary<BotCommand, CommandDictionaryValue> _commands;
        private readonly Dictionary<UpdateType, Func<ITelegramBotClient, Update, CancellationToken, Task>> _types;
        private readonly Dictionary<long, BotCommand> _chatHistory = [];
        private readonly IEnumerable<string> _authUsers = [];

        public ChatService(ILogger<ChatService> logger, IConfiguration config, ICommandService commandService)
        {
            _logger = logger;
            _config = config;
            _commands = commandService.GetCommands();

            var authUsers = _config[EnvironmentVariables.AUTH_USERS]?.Split(",");
            ArgumentNullException.ThrowIfNull(authUsers);

            _authUsers = authUsers;

            _types = new Dictionary<UpdateType, Func<ITelegramBotClient, Update, CancellationToken, Task>>
            {
                { UpdateType.Message, HandleMessageUpdate }
            };
        }

        public async Task ListenAsync()
        {
            try
            {
                var token = _config[EnvironmentVariables.BOT_TOKEN];
                ArgumentNullException.ThrowIfNull(token);

                var botClient = new TelegramBotClient(token);
                using var cts = new CancellationTokenSource();

                ReceiverOptions receiverOptions = new()
                {
                    AllowedUpdates = [.. _types.Keys]
                };

                await botClient.SetMyCommandsAsync(_commands.Keys);
                botClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    pollingErrorHandler: HandlePollingErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: cts.Token
                );

                var me = await botClient.GetMeAsync();
                _logger.LogInformation($"Start listening for @{me.Username}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start listening: {ex.Message}");
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(errorMessage);
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;

            if (!_authUsers.Contains(message.Chat.Username))
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: Answers.Common.FORBIDDEN,
                    cancellationToken: cancellationToken);
            }
            else if (_types.TryGetValue(update.Type, out var method))
            {
                await method.Invoke(botClient, update, cancellationToken);
            }
        }

        private async Task HandleMessageUpdate(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;

            if (message.Text is not { } text)
                return;

            var chatId = message.Chat.Id;

            var prevComamnd = _chatHistory.GetValueOrDefault(chatId);
            var command = _commands.Keys.FirstOrDefault(c => c.Command == text);

            if (command != null)
            {
                var value = _commands[command];

                if (prevComamnd == null)
                {
                    _chatHistory.Add(chatId, command);
                }
                else
                {
                    _chatHistory[chatId] = command;
                }

                await value.CommandMethod(message, botClient, cancellationToken);

                return;
            }

            if (prevComamnd != null)
            {
                var value = _commands[prevComamnd];

                if (value.AnswerMethod != null)
                {
                    await value.AnswerMethod(message, botClient, cancellationToken);
                    _chatHistory.Remove(chatId);
                }

                return;
            }
        }
    }
}
