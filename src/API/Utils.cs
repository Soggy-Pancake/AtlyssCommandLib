using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AtlyssCommandLib.src.API;
using UnityEngine;
using static AtlyssCommandLib.API.CommandProvider;

namespace AtlyssCommandLib.API;

/// <summary>
/// Utility functions for the Atlyss Command Library.
/// </summary>
public class Utils {

    /// <summary>
    /// Registers a new command.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="helpMessage"></param>
    /// <param name="callback"></param>
    /// <param name="clientSide"></param>
    /// <param name="serverSide"></param>
    /// <param name="console"></param>
    /// <returns></returns>
    public static ModCommand? RegisterCommand(string command, string helpMessage, CommandCallback callback, bool clientSide = true, bool serverSide = false, bool console = false)
        => CommandManager.root.RegisterCommand(command, helpMessage, callback, clientSide, serverSide, console);

    /// <summary>
    /// Registers a new command with detailed help.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="helpMessage"></param>
    /// <param name="detailedHelpMessage"></param>
    /// <param name="callback"></param>
    /// <param name="clientSide"></param>
    /// <param name="serverSide"></param>
    /// <param name="console"></param>
    /// <returns></returns>
    public static ModCommand? RegisterCommand(string command, string helpMessage, string detailedHelpMessage, CommandCallback callback, bool clientSide = true, bool serverSide = false, bool console = false)
        => CommandManager.root.RegisterCommand(command, helpMessage, detailedHelpMessage, callback, clientSide, serverSide, console);

    /// <summary>
    /// Registers a new command with CommandOptions.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="helpMessage"></param>
    /// <param name="callback"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static ModCommand? RegisterCommand(string command, string helpMessage, CommandCallback callback, CommandOptions options)
        => CommandManager.root.RegisterCommand(command, helpMessage, callback, options);

    /// <summary>
    /// Registers a new command with detailed help and CommandOptions.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="helpMessage"></param>
    /// <param name="detailedHelpMessage"></param>
    /// <param name="callback"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static ModCommand? RegisterCommand(string command, string helpMessage, string detailedHelpMessage, CommandCallback callback, CommandOptions options)
        => CommandManager.root.RegisterCommand(command, helpMessage, detailedHelpMessage, callback, options);

    /// <summary>
    /// Registers a command given a ModCommand object.
    /// </summary>
    /// <param name="command"></param>
    public static void RegisterCommand(ModCommand command)
        => CommandManager.root.RegisterCommand(command);

    /// <summary>
    /// Registers a new CommandProvider.
    /// </summary>
    /// <param name="provider"></param>
    public static void RegisterProvider(CommandProvider provider)
        => CommandManager.root.RegisterProvider(provider);

    /// <summary>
    /// Registers a new alias for a command.
    /// </summary>
    /// <param name="alias"></param>
    /// <param name="command"></param>
    public static void RegisterAlias(string alias, ModCommand? command)
        => CommandManager.root.RegisterAlias(alias, command);

    /// <summary>
    /// Registers multiple aliases for a command.
    /// </summary>
    /// <param name="aliases"></param>
    /// <param name="command"></param>
    public static void RegisterAlias(string[] aliases, ModCommand? command)
        => CommandManager.root.RegisterAlias(aliases, command);

    /// <summary>
    /// Registers a new alias for a CommandProvider.
    /// </summary>
    /// <param name="alias"></param>
    /// <param name="provider"></param>
    public static void RegisterAlias(string alias, CommandProvider? provider)
        => CommandManager.root.RegisterAlias(alias, provider);

    /// <summary>
    /// Registers multiple aliases for a CommandProvider.
    /// </summary>
    /// <param name="aliases"></param>
    /// <param name="provider"></param>
    public static void RegisterAlias(string[] aliases, CommandProvider? provider)
        => CommandManager.root.RegisterAlias(aliases, provider);

    /// <summary>
    /// Sends a message to the caller.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="message"></param>
    /// <param name="color"></param>
    public static void NotifyCaller(Caller caller, string message, Color color = default) {
        if (caller.isConsole) {
            Plugin.getHostConsole();
            Plugin.hostConsole.NC()?.New_LogMessage(message);
        } else if (caller.player == Player._mainPlayer) {
            if (color == default)
                color = Color.white;
            Player._mainPlayer._chatBehaviour.New_ChatMessage($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>");
        } else {
            if (color == default)
                color = Color.white;
            caller.player?._chatBehaviour.Target_RecieveMessage($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{message}</color>");
        }
    }

    /// <summary>
    /// Creates the help message for a CommandProvider.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static string buildHelpMessage(Caller caller, CommandProvider provider) {
        string[] getCommands(CommandProvider provider) {
            bool remotePlayer = caller.IsRemote;

            return provider.commands
                        .Where(cmd => {
                                var o = cmd.Value.options;
                                return (caller.isConsole && (o.consoleCmd || o.serverSide)) ||
                                        (caller.isHost && (o.serverSide || o.hostOnly)) ||
                                        (!remotePlayer && o.clientSide);
                                })
                        .Select(cmd => cmd.Key)
                        .Concat(provider.childProviders
                            .Where(pbj => {
                                var p = pbj.Value;
                                return (caller.isConsole && (p.hasConsoleCommands || p.hasServerCommands)) ||
                                        (caller.isHost && (p.hasServerCommands || p.hasHostOnlyCommands)) ||
                                        (!remotePlayer && p.hasClientCommands);
                            })
                            .Select(s => s.Key))
                        .Distinct().ToArray();
        }

        string[] availableCommands = getCommands(provider);
        if (provider == Root)
            availableCommands = availableCommands.Concat(CommandManager.serverCommands.Keys).Distinct().ToArray();

        int maxCmdLength = availableCommands.Max(c => c.Length);
        maxCmdLength = ((maxCmdLength - 1) / 4 + 1) * 4;

        string cmds = "Available commands: \n";
        foreach (var cmd in availableCommands)
            cmds += "/" + cmd.PadRight(maxCmdLength) + " - " +
                                           ((provider.commands.ContainsKey(cmd) ? provider.commands[cmd].getHelpMessage() : null) ??
                                            (provider.childProviders.ContainsKey(cmd) ? provider.childProviders[cmd]?.getHelpMessage() : "ERROR")) + "\n";
        return cmds.TrimEnd();
    }
}
