namespace Ryhor.Bot.Services.Interfaces
{
    public interface IChatService : IService
    {
        Task ListenAsync();
    }
}
