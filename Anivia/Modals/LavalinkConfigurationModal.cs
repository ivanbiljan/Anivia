using Discord;
using Discord.Interactions;

namespace Anivia.Modals;

public sealed class LavalinkConfigurationModal : IModal
{
    public string Title => "Lavalink Server Configuration";
    
    [ModalTextInput("hostname", TextInputStyle.Short, placeholder: "Server IP / Domain")]
    public string Hostname { get; set; }
    
    [ModalTextInput("port", TextInputStyle.Short, placeholder:"1234")]
    public string Port { get; set; }
    
    [ModalTextInput("password")]
    public string Password { get; set; }

    [ModalTextInput("isSsl", TextInputStyle.Short, initValue:"true")]
    public string IsSsl { get; set; }
}