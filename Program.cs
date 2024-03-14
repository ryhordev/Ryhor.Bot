using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ryhor.Bot.Services.Implementations;
using Ryhor.Bot.Services.Interfaces;

var builder = Host.CreateDefaultBuilder(args)
    .UseEnvironment(Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") ?? "Production")
    .ConfigureLogging(log => log.AddConsole())
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<ICommandService, CommandService>();
    });

var host = builder.Build();
var chatService = host.Services.GetService<IChatService>();

ArgumentNullException.ThrowIfNull(chatService);
await chatService.ListenAsync();
await host.RunAsync();