using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace AtlyssCommandLib.API;

/// <summary>
/// Options for a command. This is a struct to avoid reference issues so you can reuse this without worrying.
/// </summary>
public struct CommandOptions {
    /// <summary>
    /// Whenever this command can be used in chat
    /// </summary>
    public ChatCommandType chatCommand;

    /// <summary>
    /// Whenever this command can be called from the host console.
    /// </summary>
    public bool consoleCmd;

    /// <summary>
    /// Whenever this command is implemented by vanilla or another mod that doesn't use CommandLib.
    /// </summary>
    internal bool mustRunVanillaCode;

    /// <summary>
    /// Default options: chatCommand = Local, consoleCmd = false
    /// </summary>
    public CommandOptions() {
        chatCommand = ChatCommandType.ClientSide;
        consoleCmd = false;
    }
    
    /// <summary>
    /// Default options: consoleCmd = false
    /// </summary>
    public CommandOptions(ChatCommandType chatCommand) {
        this.chatCommand = chatCommand;
        this.consoleCmd = false;
    }

    /// <summary>
    /// Creates command options
    /// </summary>
    public CommandOptions(ChatCommandType chatCommand, bool consoleCmd) {
        this.chatCommand = chatCommand;
        this.consoleCmd = consoleCmd;
    }
    
    /// <summary>
    /// Full optional constructor, everything defaults to false
    /// </summary>
    /// <param name="clientSide"></param>
    /// <param name="serverSide"></param>
    /// <param name="consoleCmd"></param>
    /// <param name="hostOnly"></param>
    [Obsolete("Use the ChatCommandType variant")]
    public CommandOptions(bool clientSide = false, bool serverSide = false, bool consoleCmd = false, bool hostOnly = false) {
        this.consoleCmd = consoleCmd;
        if (hostOnly)
            chatCommand = ChatCommandType.HostOnly;
        else if (serverSide)
            chatCommand = ChatCommandType.ServerSide;
        else if (clientSide)
            chatCommand = ChatCommandType.ClientSide;
        else
            chatCommand = ChatCommandType.None;
    }

    public override string ToString()
    {
        return $"[chatCommand = {chatCommand}, consoleCmd = {consoleCmd}]";
    }
}
