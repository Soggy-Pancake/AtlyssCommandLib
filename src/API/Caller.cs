using System;
using System.Collections.Generic;
using System.Text;

namespace AtlyssCommandLib.API;

/// <summary>
/// Information about who called a command.
/// </summary>
public struct Caller {
    /// <summary>
    /// The player who called the command. Null if called from console.
    /// </summary>
    public Player? player;

    /// <summary>
    /// Whether the caller is the host player.
    /// </summary>
    public bool isHost => player?.Network_isHostPlayer ?? false || isConsole;

    /// <summary>
    /// Whether the caller is the console.
    /// </summary>
    public readonly bool isConsole;

    /// <summary>
    /// Whether the caller is a remote player.
    /// </summary>
    public readonly bool IsRemote => player != Player._mainPlayer;

    internal string cmdPrefix;

    internal Caller(string prefix, Player? _player, bool console = false)
    {
        cmdPrefix = prefix;
        player = _player;
        isConsole = console;
    }

    internal Caller(string prefix, ChatBehaviour chatBehaviour) {
        cmdPrefix = prefix;
        player = chatBehaviour.GetComponent<Player>();
    }

    public override string ToString() => $"Player {player?._nickname} (console {isConsole}, remote {IsRemote})";
}
