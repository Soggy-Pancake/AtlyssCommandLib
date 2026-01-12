using System;
using System.Collections.Generic;
using System.Linq;
using AtlyssCommandLib.API;
using CodeTalker.Packets;
using UnityEngine;
using UnityEngine.Profiling;
using static AtlyssCommandLib.API.Utils;

namespace AtlyssCommandLib;
internal class CommandManager {

    internal static readonly CommandProvider root = new CommandProvider("/", "");

    // commands local to the server [prefix, helpMessage]
    internal static Dictionary<string, string> serverCommands = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    internal static bool CommandIsInstalled(string command)
    {
        if (command == "chatcolor")
            return Plugin.chatColorsInstalled;
        
        return true;
    }

    internal static void updateServerCommands() {
        if(Player._mainPlayer.NC()?.Network_isHostPlayer ?? false)
            return;

        serverCommands.Clear();
        foreach (var cmd in root.commands.Values) {
            if (cmd.options.chatCommand == ChatCommandType.ServerSide && CommandIsInstalled(cmd.Command)) {
                serverCommands[cmd.Command] = cmd.getHelpMessage();
            }
        }

        foreach (var provider in root.childProviders.Values) {
            if (provider.hasServerCommands) { 
                serverCommands[provider.prefix] = provider.getHelpMessage();
            }
        }
    }

    internal static void updateServerCommands(PacketHeader header, BinaryPacketBase pkt) { 
        if(Player._mainPlayer.NC()?.Network_isHostPlayer == false && pkt is ServerCommandPkt packet) {
            serverCommands.Clear();
            foreach (var entry in packet.entries)
                serverCommands[entry.prefix] = entry.helpMessage;
        }
    }

    internal static bool execCommand(ModCommand cmd, Caller caller, string[] args) {
        try
        {
            Plugin.logger?.LogDebug($"Processing command {caller.cmdPrefix} - options {cmd.options} - caller {caller}");
            bool success = cmd.Callback(caller, args);
            if (!success)
                cmd.printHelp(caller);
            return success;
        } catch (ArgumentException e) {
            if (!string.IsNullOrEmpty(e.Message) && string.IsNullOrEmpty(e.ParamName)) {
                NotifyCaller(caller, $"Recieved invalid arguments for command '{caller.cmdPrefix} {string.Join(" ", args)}' Reason: " + e.Message, Color.yellow);
            } else if (!string.IsNullOrEmpty(e.ParamName) && !string.IsNullOrEmpty(e.Message)) {
                NotifyCaller(caller, $"'{e.ParamName}' is invalid! Reason: {e.Message}");
            } else
                Plugin.logger?.LogError($"Recieved invalid arguments for command '{caller.cmdPrefix} {string.Join(" ", args)}' Error: " + e);

            cmd.printHelp(caller);
            return false;
        } catch (Exception e) {
            Plugin.logger?.LogError($"Error executing command '{caller.cmdPrefix} {string.Join(" ", args)}' Error: " + e);
            cmd.printHelp(caller);
            return false;
        }
    }
}
