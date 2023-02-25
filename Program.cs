using Corner.Failsafe.Discord;
using Corner.Failsafe.Newsfeed;
using Discord.WebSocket;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var configurationRoot = context.Configuration;

        services.AddSingleton<DiscordSocketClient>();

        services.AddOptions<DiscordSocketOptions>()
            .Bind(configurationRoot.GetRequiredSection("Discord"))
            .ValidateDataAnnotations();
        services.AddHostedService<DiscordSocketService>();

        services.AddOptions<NewsfeedOptions>()
            .Bind(configurationRoot.GetRequiredSection("Newsfeed"))
            .ValidateDataAnnotations();
        services.AddHostedService<NewsfeedService>();
    })
    .Build();

host.Run();
