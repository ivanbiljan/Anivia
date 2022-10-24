using Discord;
using Discord.Interactions;

namespace Anivia.Modals;

public sealed class LavalinkConfigurationModal : IModal
{
    [ModalTextInput("hostname", TextInputStyle.Short, "Server IP / Domain")]
    public string Hostname { get; set; }

    [ModalTextInput("isSsl", TextInputStyle.Short, initValue: "true")]
    public string IsSsl { get; set; }

    [ModalTextInput("password")] public string Password { get; set; }

    [ModalTextInput("port", TextInputStyle.Short, "1234")]
    public string Port { get; set; }

    public string Title => "Lavalink Server Configuration";
}