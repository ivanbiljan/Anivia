using Anivia.CommandModules;
using Anivia.Extensions;
using Anivia.Options;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Microsoft.Extensions.Options;
using Victoria;
using Victoria.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.ConfigureAuditableOptions<DiscordOptions>(
    builder.Configuration.GetSection(DiscordOptions.SectionName)
);

builder.Services.ConfigureAuditableOptions<LavalinkOptions>(
    builder.Configuration.GetSection(LavalinkOptions.SectionName)
);

builder.Services.AddSingleton(
        _ => new DiscordSocketClient(
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

builder.Services.AddSingleton<InteractionService>();

builder.Services.AddSingleton(
        new InteractiveConfig
        {
            DefaultTimeout = TimeSpan.FromMinutes(1)
        }
    )
    .AddSingleton<InteractiveService>();

builder.Services.AddLavaNode(
    options =>
    {
        var lavalinkOptions = builder.Configuration.GetSection(LavalinkOptions.SectionName).Get<LavalinkOptions>();

        options.Hostname = lavalinkOptions.Host;
        options.Port = lavalinkOptions.Port;
        options.Authorization = lavalinkOptions.Password;
        options.IsSecure = lavalinkOptions.IsSsl;
    }
);

var app = builder.Build();

var commandService = app.Services.GetRequiredService<CommandService>();
await commandService.AddModulesAsync(typeof(Program).Assembly, app.Services);

var lavaNode = app.Services.GetRequiredService<LavaNode>();
lavaNode.OnTrackEnd += async args =>
{
    if (args.Reason is not TrackEndReason.Finished)
    {
        return;
    }

    var discordClient = app.Services.GetRequiredService<DiscordSocketClient>();
    var discordOptions = app.Services.GetRequiredService<IAuditableOptionsSnapshot<DiscordOptions>>().CurrentValue;
    var textChannel = (IMessageChannel) await discordClient.GetChannelAsync(discordOptions.TextChannelId);
    var player = await lavaNode.GetPlayerAsync(args.GuildId);
    var queue = player.GetCustomQueue();

    if (queue.IsCurrentTrackLooped)
    {
        await player.PlayAsync(lavaNode, queue.Current);

        return;
    }

    if (queue.Next is null && !queue.IsLooped)
    {
        queue.Clear();
        await textChannel.SendMessageAsync(embed: Embeds.Error("There are no more tracks"));

        return;
    }

    var track = queue.GetNext()!;
    await player.PlayAsync(lavaNode, track);
};

lavaNode.OnTrackStart += async args =>
{
    var embed = new EmbedBuilder()
        .WithDescription($"Started playing [{args.Track.Title}]({args.Track.Url})")
        .Build();

    var discordClient = app.Services.GetRequiredService<DiscordSocketClient>();
    var discordOptions = app.Services.GetRequiredService<IAuditableOptionsSnapshot<DiscordOptions>>().CurrentValue;
    var textChannel = (IMessageChannel) await discordClient.GetChannelAsync(discordOptions.TextChannelId);

    await textChannel.SendMessageAsync(embed: embed);
};

var client = app.Services.GetRequiredService<DiscordSocketClient>();
client.Ready += async () =>
{
    if (!lavaNode.IsConnected)
    {
        try
        {
            Console.WriteLine("Connecting");
            await lavaNode.ConnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    var interactionService = new InteractionService(client.Rest);
    await interactionService.AddModulesAsync(typeof(Program).Assembly, app.Services);
    await interactionService.RegisterCommandsGloballyAsync();

    client.InteractionCreated += async interaction =>
    {
        var ctx = new SocketInteractionContext(client, interaction);
        await interactionService.ExecuteCommandAsync(ctx, app.Services);
    };
};

client.MessageReceived += async message =>
{
    // Don't process the command if it was a system message
    if (message is not SocketUserMessage socketUserMessage)
    {
        return;
    }

    // Create a number to track where the prefix ends and the command begins
    var argPos = 0;

    // Determine if the message is a command based on the prefix and make sure no bots trigger commands
    var commandPrefixes = app.Services.GetRequiredService<IOptionsMonitor<DiscordOptions>>()
        .CurrentValue
        .CommandPrefixes;

    if ((!commandPrefixes.Any(p => socketUserMessage.HasStringPrefix(p, ref argPos)) &&
         !socketUserMessage.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
        socketUserMessage.Author.IsBot)
    {
        return;
    }

    // Create a WebSocket-based command context based on the message
    var context = new SocketCommandContext(client, socketUserMessage);

    // Execute the command with the command context we just
    // created, along with the service provider for precondition checks.
    var result = await commandService.ExecuteAsync(
        context,
        argPos,
        app.Services
    );
};

client.MessageUpdated += async (_, message, _) =>
{
    // Don't process the command if it was a system message
    if (message is not SocketUserMessage socketUserMessage)
    {
        return;
    }

    // Create a number to track where the prefix ends and the command begins
    var argPos = 0;

    // Determine if the message is a command based on the prefix and make sure no bots trigger commands
    var commandPrefixes = app.Services.GetRequiredService<IOptionsMonitor<DiscordOptions>>()
        .CurrentValue
        .CommandPrefixes;

    if ((!commandPrefixes.Any(p => socketUserMessage.HasStringPrefix(p, ref argPos)) &&
         !socketUserMessage.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
        socketUserMessage.Author.IsBot)
    {
        return;
    }

    // Create a WebSocket-based command context based on the message
    var context = new SocketCommandContext(client, socketUserMessage);

    // Execute the command with the command context we just
    // created, along with the service provider for precondition checks.
    await commandService.ExecuteAsync(
        context,
        argPos,
        app.Services
    );
};

client.UserVoiceStateUpdated += async (user, state, _) =>
{
    var player = await lavaNode.GetPlayerAsync(state.VoiceChannel.Guild.Id);
    if (player is null || player.VoiceState.SessionId != state.VoiceSessionId)
    {
        return;
    }

    var discordClient = app.Services.GetRequiredService<DiscordSocketClient>();
    var discordOptions = app.Services.GetRequiredService<IAuditableOptionsSnapshot<DiscordOptions>>().CurrentValue;
    var textChannel = (IMessageChannel) await discordClient.GetChannelAsync(discordOptions.TextChannelId);

    if (user.Id == client.CurrentUser.Id)
    {
        await lavaNode.LeaveAsync(state.VoiceChannel);
        await textChannel.SendMessageAsync(embed: Embeds.Error("I have been kicked from the voice channel"));

        return;
    }

    if (state.VoiceChannel.ConnectedUsers.Count >= 2)
    {
        return;
    }

    await lavaNode.LeaveAsync(state.VoiceChannel);
    await textChannel.SendMessageAsync(embed: Embeds.Error("Stopping because everyone left"));
};

var options = builder.Configuration.GetSection(DiscordOptions.SectionName).Get<DiscordOptions>()!;
await client.LoginAsync(TokenType.Bot, options.BotToken);
await client.StartAsync();

app.MapGet("/", () => "Hello World!");

app.Run();