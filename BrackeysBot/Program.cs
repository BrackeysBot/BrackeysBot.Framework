using BrackeysBot;
using BrackeysBot.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

await Host.CreateDefaultBuilder(args)
    .ConfigureLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddNLog();
    })
    .ConfigureServices(services =>
    {
        services.AddHostedService<LoggingService>();
        services.AddHostedService<BrackeysBotApp>();
    })
    .UseConsoleLifetime()
    .RunConsoleAsync();
