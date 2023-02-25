using Corner.Failsafe.Discord;
using Discord.WebSocket;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var configurationRoot = context.Configuration;

        services.AddSingleton<DiscordSocketClient>();

        services.Configure<DiscordSocketOptions>(configurationRoot.GetRequiredSection("Discord"));
        services.AddHostedService<DiscordSocketService>();
    })
    .Build();

host.Run();
