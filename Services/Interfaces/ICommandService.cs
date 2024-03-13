using Telegram.Bot;
using Telegram.Bot.Types;

namespace Ryhor.Bot.Services.Interfaces
{
    public interface ICommandService : IService
    {
        public Dictionary<BotCommand, Func<Message, ITelegramBotClient, CancellationToken, Task>> GetCommands();
    }
}
