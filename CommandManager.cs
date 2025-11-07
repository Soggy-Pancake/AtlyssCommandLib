using System;
using System.Collections.Generic;
using System.Linq;
using AtlyssCommandLib.API;
using CodeTalker.Packets;
using UnityEngine.Profiling;
using static AtlyssCommandLib.API.Utils;

namespace AtlyssCommandLib;
internal class CommandManager {

    internal static readonly CommandProvider root = new CommandProvider("/", "");

    // commands local to the server [prefix, helpMessage]
    internal static Dictionary<string, string> serverCommands = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    internal static void updateServerCommands() {
        if(Player._mainPlayer.NC()?.Network_isHostPlayer ?? false)
            return;

        serverCommands.Clear();
        foreach (var cmd in root.commands.Values) {
            if (cmd.serverSideCommand && (cmd.Command != "chatcolor" || (cmd.Command == "chatcolor" && Plugin.chatColorsInstalled))) {
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
        bool amServer = Player._mainPlayer.NC()?.Network_isHostPlayer ?? false;

        Plugin.logger?.LogInfo("Command being called: " + caller.cmdPrefix);
        Plugin.logger?.LogInfo("Am server?: " + amServer);
        Plugin.logger?.LogInfo($"cmd: server/client/console {cmd.serverSideCommand} {cmd.clientSideCommand} {cmd.consoleCommand}");

        // Console only
        if (cmd.consoleCommand) { 
            if (caller.isConsole)
                cmd.Callback(caller, args);
            else
                Plugin.logger?.LogError($"Command '{caller.cmdPrefix}' is console only. Caller is console: {caller.isConsole}");
            return true;
        }

        // server side only
        if (!cmd.clientSideCommand && cmd.serverSideCommand && amServer) { 
            Plugin.logger?.LogInfo("Server side only command!");
            if (!cmd.Callback(caller, args))
                cmd.printHelp(caller);
            return true;
        }

        // client side only
        if (cmd.clientSideCommand && !cmd.serverSideCommand) { 
            Plugin.logger?.LogInfo("Client side only command!");
            if (!cmd.Callback(caller, args))
                cmd.printHelp(caller);
            return true;
        }

        // Server and client side
        if (!cmd.consoleCommand && cmd.serverSideCommand && cmd.clientSideCommand) {
            Plugin.logger?.LogInfo("Client and server side command!");
            bool result = cmd.Callback(caller, args);
            if (!result)
                cmd.printHelp(caller);
            return amServer || !result;
            // callback returns true to forward to server if client. Blocks if already server.
        }

        return true;
    }
}
