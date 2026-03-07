using Anivia;
using Anivia.CommandModules;
using Anivia.Extensions;
using Anivia.Infrastructure;
using Lavalink4NET.Extensions;
using Lavalink4NET.NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.Commands;
using NetCord.Services.Commands;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.ConfigureAuditableOptions<DiscordOptions>(
    builder.Configuration.GetSection(DiscordOptions.SectionName)
);

builder.Services.ConfigureAuditableOptions<LavalinkOptions>(
    builder.Configuration.GetSection(LavalinkOptions.SectionName)
);

builder.Services.AddDiscordGateway(options =>
    {
        options.Token = builder.Configuration.GetSection(DiscordOptions.SectionName)[nameof(DiscordOptions.BotToken)];
        options.Intents = GatewayIntents.AllNonPrivileged
                          | GatewayIntents.MessageContent
                          | GatewayIntents.GuildPresences
                          | GatewayIntents.GuildUsers;
    }
);

builder.Services.AddCommands<CommandContext>(options =>
    {
        var prefixes = builder.Configuration
            .GetSection(DiscordOptions.SectionName)
            .GetSection(nameof(DiscordOptions.CommandPrefixes))
            .Get<List<string>>() ?? ["!"];

        options.Prefixes = prefixes;
    }
);

builder.Services.AddLavalink();
builder.Services.ConfigureLavalink(options =>
    {
        var lavalinkConfig = builder.Configuration.GetSection(LavalinkOptions.SectionName).Get<LavalinkOptions>()!;
        options.BaseAddress = new Uri(lavalinkConfig.Host);
        options.Passphrase = lavalinkConfig.Password;
    }
);

builder.Services.AddSingleton<PlaybackEventListener>();
builder.Services.AddSingleton<HelpPaginator>();

builder.Services.AddGatewayHandler<VoiceStateHandler>();
builder.Services.AddGatewayHandler<ReadyHandler>();
builder.Services.AddGatewayHandler<HelpPaginator>();

var app = builder.Build();

app.AddModules(typeof(Program).Assembly);

await app.RunAsync();