using Anivia;
using Anivia.Extensions;
using Anivia.Infrastructure;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Victoria;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.ConfigureAuditableOptions<DiscordOptions>(
    builder.Configuration.GetSection(DiscordOptions.SectionName)
);

builder.Services.ConfigureAuditableOptions<LavalinkOptions>(
    builder.Configuration.GetSection(LavalinkOptions.SectionName)
);

builder.Services.AddSingleton(_ => new DiscordSocketClient(
            new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged |
                                 GatewayIntents.MessageContent |
                                 GatewayIntents.GuildPresences |
                                 GatewayIntents.GuildMembers
            }
        )
    )
    .AddSingleton<IDiscordClient>(provider => provider.GetRequiredService<DiscordSocketClient>());

builder.Services.AddSingleton<CommandService>();

builder.Services.AddSingleton<InteractionService>(provider =>
    {
        var client = provider.GetRequiredService<DiscordSocketClient>();

        return new InteractionService(client);
    }
);

builder.Services.AddSingleton(
        new InteractiveConfig
        {
            DefaultTimeout = TimeSpan.FromMinutes(1)
        }
    )
    .AddSingleton<InteractiveService>();

builder.Services.AddLavaNode(options =>
    {
        var lavaLinkOptions = builder.Configuration.GetSection(LavalinkOptions.SectionName).Get<LavalinkOptions>()!;
        options.Hostname = lavaLinkOptions.Host;
        options.Port = lavaLinkOptions.Port;
        options.Authorization = lavaLinkOptions.Password;
    }
);

var app = builder.Build();
var bootstrapper = app.Services.GetRequiredService<Bootstrapper>();
await bootstrapper.BootstrapAsync();

app.MapGet("/", () => "Hello World!");

app.Run();