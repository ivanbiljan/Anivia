// See https://aka.ms/new-console-template for more information

using System;
using System.Linq;
using Anivia.CommandModules;
using Anivia.Extensions;
using Anivia.Options;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Victoria;
using Victoria.Enums;

var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json")
    .Build();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureHostOptions(opts => opts.ShutdownTimeout = TimeSpan.Zero)
    .ConfigureHostConfiguration(builder => builder.AddConfiguration(configuration))
    .ConfigureServices(
        (context, services) =>
        {
            services.ConfigureAuditableOptions<DiscordOptions>(
                context.Configuration.GetSection(DiscordOptions.SectionName));

            services.AddSingleton(
                _ => new DiscordSocketClient(
                    new DiscordSocketConfig
                    {
                        GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
                    })).AddSingleton<IDiscordClient>(provider => provider.GetRequiredService<DiscordSocketClient>());

            services.AddSingleton<CommandService>();

            services.AddLavaNode(
                options =>
                {
                    var lavalinkOptions = configuration.GetSection(LavalinkOptions.SectionName).Get<LavalinkOptions>();

                    options.Hostname = lavalinkOptions.Host;
                    options.Port = lavalinkOptions.Port;
                    options.Authorization = lavalinkOptions.Password;
                    options.IsSsl = lavalinkOptions.IsSsl;
                });
        })
    .Build();

var commandService = host.Services.GetRequiredService<CommandService>();
await commandService.AddModulesAsync(typeof(Program).Assembly, host.Services);

var lavaNode = host.Services.GetRequiredService<LavaNode>();
lavaNode.OnTrackEnded += async args =>
{
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

    var track = queue.ConsumeAndAdvance()!;
    await args.Player.PlayAsync(track);
};

lavaNode.OnTrackStarted += async args =>
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
    await commandService.ExecuteAsync(
        context,
        argPos,
        host.Services);
};

var options = configuration.GetSection(DiscordOptions.SectionName).Get<DiscordOptions>();
await client.LoginAsync(TokenType.Bot, options.BotToken);
await client.StartAsync();

await host.RunAsync();