using System.Collections.Concurrent;
using System.Reflection;
using NetCord;
using NetCord.Hosting.Gateway;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace Anivia.CommandModules;

public sealed record HelpModule(string Name, List<HelpCommand> Commands);

public sealed record HelpCommand(string Name, IReadOnlyList<string> Aliases, string Description);

public sealed class HelpPaginator(CommandService<CommandContext> commandService) : IInteractionCreateGatewayHandler
{
    public const string NextId = "help_next";
    public const string PrevId = "help_prev";

    private static readonly ConcurrentDictionary<ulong, int> PageState = new();

    public async ValueTask HandleAsync(Interaction interaction)
    {
        if (interaction is not ButtonInteraction buttonInteraction)
        {
            return;
        }

        var customId = buttonInteraction.Data.CustomId;
        if (customId is not (PrevId or NextId))
        {
            return;
        }

        var messageId = buttonInteraction.Message.Id;
        var modules = GetModules();
        var totalPages = modules.Count;
        var currentPage = GetPage(messageId);

        currentPage = customId switch
        {
            PrevId => Math.Max(0, currentPage - 1),
            NextId => Math.Min(totalPages - 1, currentPage + 1),
            _      => currentPage
        };

        SetPage(messageId, currentPage);

        var embed = BuildPage(modules[currentPage], currentPage, totalPages);
        var buttons = BuildButtons(currentPage, totalPages);

        await buttonInteraction.SendResponseAsync(
            InteractionCallback.ModifyMessage(m =>
                {
                    m.Embeds = [embed];
                    m.Components = buttons;
                }
            )
        );
    }

    public static IMessageComponentProperties[] BuildButtons(int pageIndex, int totalPages)
    {
        return
        [
            new ActionRowProperties(
                [
                    new ButtonProperties(PrevId, "◀", ButtonStyle.Secondary) {Disabled = pageIndex <= 0},
                    new ButtonProperties(NextId, "▶", ButtonStyle.Secondary) {Disabled = pageIndex >= totalPages - 1}
                ]
            )
        ];
    }

    public static EmbedProperties BuildPage(HelpModule module, int pageIndex, int totalPages)
    {
        var commandSummaries = string.Join(
            Environment.NewLine,
            module.Commands.Select(c =>
                {
                    var aliases = c.Aliases.Count > 1
                        ? $" ({string.Join(", ", c.Aliases.Skip(1))})"
                        : string.Empty;

                    return $"**{c.Name}**{aliases} - {c.Description}";
                }
            )
        );

        return new EmbedProperties
        {
            Title = module.Name,
            Description = commandSummaries,
            Footer = new EmbedFooterProperties {Text = $"Page {pageIndex + 1}/{totalPages}"},
            Color = new Color(0x00FF00)
        };
    }

    public static int GetPage(ulong messageId)
    {
        return PageState.GetValueOrDefault(messageId, 0);
    }

    public static void RemovePage(ulong messageId)
    {
        PageState.TryRemove(messageId, out _);
    }

    public static void SetPage(ulong messageId, int page)
    {
        PageState[messageId] = page;
    }

    private static string GetCategory(ICommandInfo<CommandContext> commandInfo)
    {
        var attr =
            (commandInfo.Attributes.GetValueOrDefault(typeof(AniviaCommandAttribute)) ?? Array.Empty<Attribute>())
            .ElementAtOrDefault(0) as AniviaCommandAttribute;
        
        return attr?.Category ?? "Other";
    }

    public List<HelpModule> GetModules()
    {
        var commands = commandService.GetCommands();
        var moduleMap = new Dictionary<string, HelpModule>();

        foreach (var (_, commandInfos) in commands)
        {
            foreach (var commandInfo in commandInfos)
            {
                var moduleName = GetCategory(commandInfo);

                if (!moduleMap.TryGetValue(moduleName, out var module))
                {
                    module = new HelpModule(moduleName, []);
                    moduleMap[moduleName] = module;
                }

                var aliases = commandInfo.GetType()
                    .GetProperty("Aliases", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?
                    .GetValue(commandInfo) as IReadOnlyList<string>;

                var name = aliases?.FirstOrDefault() ?? "unknown";

                var description = ((AniviaCommandAttribute?)
                    (commandInfo.Attributes.GetValueOrDefault(typeof(AniviaCommandAttribute)) ??
                     Array.Empty<Attribute>()).ElementAtOrDefault(0)!).Description ?? "N/A";

                if (module.Commands.All(c => c.Name != name))
                {
                    module.Commands.Add(new HelpCommand(name, aliases ?? [name], description));
                }
            }
        }

        return [.. moduleMap.Values];
    }
}