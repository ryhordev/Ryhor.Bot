using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ryhor.Bot.Helpers.Constants;
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

            _botCommands.Add(new BotCommand
            {
                Command = BotConstants.CommandRoute.START,
                Description = BotConstants.CommandDescription.START
            }, StartComamndHandler);
        }

        public Dictionary<BotCommand, Func<Message, ITelegramBotClient, CancellationToken, Task>> GetCommands()
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
    }
}
