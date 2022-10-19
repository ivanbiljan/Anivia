using Anivia.Modals;
using Anivia.Options;
using Discord;
using Discord.Interactions;
using Victoria;

namespace Anivia.CommandModules;

public sealed class ConfigurationModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly LavaNode _lavaNode;
    private readonly LavaConfig _lavaConfig;
    private readonly IAuditableOptionsSnapshot<LavalinkOptions> _lavalinkOptions;

    public ConfigurationModule(IAuditableOptionsSnapshot<LavalinkOptions> lavalinkOptions, LavaNode lavaNode, LavaConfig lavaConfig)
    {
        _lavalinkOptions = lavalinkOptions;
        _lavaNode = lavaNode;
        _lavaConfig = lavaConfig;
    }

    [SlashCommand("config-lavalink", "Configures the lavalink server")]
    public async Task DisplayLavalinkConfigurationModalAsync()
    {
        await RespondWithModalAsync<LavalinkConfigurationModal>("lavalinkconfig");
    }

    [ModalInteraction("lavalinkconfig")]
    public async Task ConfigureLavalinkAsync(LavalinkConfigurationModal modal)
    {
        if (!ushort.TryParse(modal.Port, out var port))
        {
            await RespondAsync(embed: Embeds.Error($"Cannot parse port {modal.Port}"));

            return;
        }

        var isSsl = bool.TryParse(modal.IsSsl, out var val) && val;
        
        if (_lavaNode.IsConnected)
        {
            await _lavaNode.DisconnectAsync();
        }

        var oldConfig = new LavaConfig
        {
            Hostname = _lavaConfig.Hostname,
            Port = _lavaConfig.Port,
            Authorization = _lavaConfig.Authorization,
            IsSsl = _lavaConfig.IsSsl
        };
        
        try
        {
            _lavaConfig.Hostname = modal.Hostname;
            _lavaConfig.Port = port;
            _lavaConfig.Authorization = modal.Password;
            _lavaConfig.IsSsl  = isSsl;

            await _lavaNode.ConnectAsync();
            
            _lavalinkOptions.Update(
                options =>
                {
                    options.Host = modal.Hostname;
                    options.Port = port;
                    options.Password = modal.Password;
                    options.IsSsl = isSsl;
                });
            
            await RespondAsync(embed: Embeds.Success("Server updated"));
        }
        catch (Exception ex)
        {
            _lavaConfig.Hostname = oldConfig.Hostname;
            _lavaConfig.Port = oldConfig.Port;
            _lavaConfig.Authorization = oldConfig.Authorization;
            _lavaConfig.IsSsl  = oldConfig.IsSsl;

            if (_lavaNode.IsConnected)
            {
                await _lavaNode.DisconnectAsync();
            }

            await _lavaNode.ConnectAsync();
            
            await RespondAsync(embed: Embeds.Error(ex.Message));
        }
    }
}