using Telegram.Bot;
using Telegram.Bot.Types;

namespace Ryhor.Bot.Entities
{
    public class CommandDictionaryValue
    {
        public required Func<Message, ITelegramBotClient, CancellationToken, Task> CommandMethod { get; set; }
        public Func<Message, ITelegramBotClient, CancellationToken, Task>? AnswerMethod { get; set; }
    }
}
