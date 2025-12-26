using Anivia;
using Anivia.Extensions;
using Anivia.Infrastructure;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Lavalink4NET.Extensions;

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

builder.Services.AddLavalink();
builder.Services.ConfigureLavalink(options =>
{
    var lavalinkConfig = builder.Configuration.GetSection(LavalinkOptions.SectionName).Get<LavalinkOptions>()!;
    options.BaseAddress = new Uri(lavalinkConfig.Host);
    options.Passphrase = lavalinkConfig.Password;
});

builder.Services.AddSingleton<Bootstrapper>();
builder.Services.AddSingleton<PlaybackEventListener>();

var app = builder.Build();
var bootstrapper = app.Services.GetRequiredService<Bootstrapper>();
await bootstrapper.InitializeAsync();

app.Run();