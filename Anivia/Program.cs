// See https://aka.ms/new-console-template for more information

using Anivia.CommandModules;
using Anivia.Extensions;
using Anivia.Options;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Fergun.Interactive;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Victoria;
using Victoria.Node;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json", true)
    .AddJsonFile("appsettings.Development.json", true)
    .Build();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureHostOptions(opts => opts.ShutdownTimeout = TimeSpan.Zero)
    .ConfigureHostConfiguration(builder => builder.AddConfiguration(configuration))
    .ConfigureServices(
        (context, services) =>
        {
            services.ConfigureAuditableOptions<DiscordOptions>(
                context.Configuration.GetSection(DiscordOptions.SectionName));

            services.ConfigureAuditableOptions<LavalinkOptions>(
                context.Configuration.GetSection(LavalinkOptions.SectionName));

            services.AddSingleton(
                _ => new DiscordSocketClient(
                    new DiscordSocketConfig
                    {
                        GatewayIntents = GatewayIntents.AllUnprivileged |
                                         GatewayIntents.MessageContent |
                                         GatewayIntents.GuildPresences |
                                         GatewayIntents.GuildMembers
                    })).AddSingleton<IDiscordClient>(provider => provider.GetRequiredService<DiscordSocketClient>());

            services.AddSingleton<CommandService>();
            
            services.AddSingleton<InteractionService>();

            services.AddSingleton(
                    new InteractiveConfig
                    {
                        DefaultTimeout = TimeSpan.FromMinutes(1)
                    })
                .AddSingleton<InteractiveService>();

            services.AddLavaNode(
                options =>
                {
                    var lavalinkOptions = configuration.GetSection(LavalinkOptions.SectionName).Get<LavalinkOptions>();

                    options.Hostname = lavalinkOptions.Host;
                    options.Port = lavalinkOptions.Port;
                    options.Authorization = lavalinkOptions.Password;
                    options.IsSecure = lavalinkOptions.IsSsl;
                });
        })
    .Build();

var commandService = host.Services.GetRequiredService<CommandService>();
await commandService.AddModulesAsync(typeof(Program).Assembly, host.Services);

var lavaNode = host.Services.GetRequiredService<LavaNode>();
lavaNode.OnTrackEnd += async args =>
{
    if (!args.Player.IsConnected)
    {
        return;
    }

    var queue = args.Player.GetQueue();
    if (queue.IsCurrentTrackLooped)
    {
        await args.Player.PlayAsync(args.Track);

        return;
    }

    if (queue.Next is null && !queue.IsLooped)
    {
        queue.Clear();
        await args.Player.TextChannel.SendMessageAsync(embed: Embeds.Error("There are no more tracks"));

        return;
    }

    var track = queue.GetNext()!;
    await args.Player.PlayAsync(track);
};

lavaNode.OnTrackStart += async args =>
{
    var embed = new EmbedBuilder()
        .WithDescription($"Started playing [{args.Track.Title}]({args.Track.Url})")
        .Build();

    await args.Player.TextChannel.SendMessageAsync(embed: embed);
};

var client = host.Services.GetRequiredService<DiscordSocketClient>();
client.Ready += async () =>
{
    if (!lavaNode.IsConnected)
    {
        await lavaNode.ConnectAsync();
    }

    var interactionService = new InteractionService(client.Rest);
    await interactionService.AddModulesAsync(typeof(Program).Assembly, host.Services);
    await interactionService.RegisterCommandsGloballyAsync();

    client.InteractionCreated += async interaction =>
    {
        var ctx = new SocketInteractionContext(client, interaction);
        await interactionService.ExecuteCommandAsync(ctx, host.Services);
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
    var commandPrefixes = host.Services.GetRequiredService<IOptionsMonitor<DiscordOptions>>().CurrentValue
        .CommandPrefixes;

    if (!commandPrefixes.Any(p => socketUserMessage.HasStringPrefix(p, ref argPos)) &&
        !socketUserMessage.HasMentionPrefix(client.CurrentUser, ref argPos) ||
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
        host.Services);
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
    var commandPrefixes = host.Services.GetRequiredService<IOptionsMonitor<DiscordOptions>>().CurrentValue
        .CommandPrefixes;

    if (!commandPrefixes.Any(p => socketUserMessage.HasStringPrefix(p, ref argPos)) &&
        !socketUserMessage.HasMentionPrefix(client.CurrentUser, ref argPos) ||
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
        host.Services);
};

client.UserVoiceStateUpdated += async (user, state, _) =>
{
    if (!lavaNode.TryGetPlayer(state.VoiceChannel.Guild, out var player))
    {
        return;
    }

    if (player.VoiceChannel.Id != state.VoiceChannel.Id)
    {
        return;
    }

    if (user.Id == client.CurrentUser.Id)
    {
        await lavaNode.LeaveAsync(player.VoiceChannel);
        await player.TextChannel.SendMessageAsync(embed: Embeds.Error("I have been kicked from the voice channel"));

        return;
    }

    if (state.VoiceChannel.ConnectedUsers.Count >= 2)
    {
        return;
    }

    await lavaNode.LeaveAsync(player.VoiceChannel);
    await player.TextChannel.SendMessageAsync(embed: Embeds.Error("Stopping because everyone left"));
};

var options = configuration.GetSection(DiscordOptions.SectionName).Get<DiscordOptions>();
await client.LoginAsync(TokenType.Bot, options.BotToken);
await client.StartAsync();

await host.RunAsync();