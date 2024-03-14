using Ryhor.Bot.Entities;
using Telegram.Bot.Types;

namespace Ryhor.Bot.Services.Interfaces
{
    public interface ICommandService : IService
    {
        public Dictionary<BotCommand, CommandDictionaryValue> GetCommands();
    }
}
