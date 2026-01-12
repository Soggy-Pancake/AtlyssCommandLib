using System.Text;

namespace AtlyssCommandLib.API;

/// <summary>
/// Options for how commands should behave in chat
/// </summary>
public enum ChatCommandType
{
    /// <summary>
    /// This command cannot be used as a chat command.
    /// </summary>
    None,
    /// <summary>
    /// This command is processed locally, and is not sent anywhere.
    /// </summary>
    ClientSide,
    /// <summary>
    /// This command is processed locally, and is not sent anywhere.
    /// Only the host has access to it.
    /// </summary>
    HostOnly,
    /// <summary>
    /// This command is processed on the server whenever clients send it.
    /// Additionally, this command will be announced to clients.
    /// </summary>
    ServerSide
}