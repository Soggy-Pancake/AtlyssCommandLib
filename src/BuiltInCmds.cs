using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Bootstrap;
using AtlyssCommandLib.API;
using static AtlyssCommandLib.CommandManager;
using static AtlyssCommandLib.API.Utils;
using UnityEngine;

namespace AtlyssCommandLib;
internal static class BuiltInCmds {
    // Commands that should be server side but need to still show if the server doesnt have it need to have both client and server side enabled
    // Im not sure how good this is as a system tho

    internal static bool Help(Caller caller, string[] args) {
        // Should support help messages for top level commands
        // also should support something like /help hb and /help hb fog or something
        // Server should give us its own help messages for server only commands so we never need to send the server a help command

        foreach (var arg in args)
            Plugin.logger?.LogInfo("Arg: " + '"' + arg + '"');

        CommandProvider provider = root;
        string currentArg = "", helpMsg;

        for (int i = 0;;) {
            Plugin.logger?.LogInfo($"help loop {i + 1} len {args.Length}: {string.Concat(args)}");
            if (i == args.Length || args[i] == "") {
                helpMsg = buildHelpMessage(caller, provider);
                string[] validAliases = [];

                try {
                    validAliases = provider.aliases
                                            .Where(a => a.Value.isProvider && a.Value.provider == provider.childProviders[currentArg])
                                            .Select(a => a.Key)
                                            .ToArray();
                } catch { }
                if (validAliases.Length > 0) {
                    helpMsg += $"\nAliases: {string.Join(", ", validAliases)}";
                }

                NotifyCaller(caller, helpMsg);
                break;
            }

            currentArg = args[i].ToLower();

            if (provider.childProviders.ContainsKey(currentArg)) {
                provider = provider.childProviders[currentArg];
                Plugin.logger?.LogInfo($"found provider {currentArg}");
                if (args.Length > 1) {
                    args = args[1..];
                } else
                    args[0] = "";
                continue;
            }

            if (provider.commands.ContainsKey(currentArg) || (provider.aliases.ContainsKey(currentArg) && !provider.aliases[currentArg].isProvider)) {
                ModCommand cmd;
                if (provider.aliases.ContainsKey(currentArg))
                    cmd = provider.aliases[currentArg].command;
                else
                    cmd = provider.commands[currentArg];

                helpMsg = cmd.getHelpMessage();
                string[] validAliases = [];
                try {
                    validAliases = provider.aliases
                                            .Where(a => !a.Value.isProvider && a.Value.command == provider.commands[currentArg])
                                            .Select(a => a.Key)
                                            .ToArray();
                } catch { }
                if (validAliases.Length > 0) {
                    helpMsg += $"\nAliases: {string.Join(", ", validAliases)}";
                }

                NotifyCaller(caller, helpMsg);
                break;
            } else {
                provider.PrintHelp(caller, currentArg);
                break;
            }
        }

        return true;
    }

    internal static bool ChatColorProtector(Caller caller, string[] args) {
        // returns true to block command if it won't work
        if (caller.isHost || caller.player != Player._mainPlayer) // We're server if we get someone else's command
            if (!Plugin.chatColorsInstalled) {
                NotifyCaller(caller, "Chat Colors mod is not installed on the server. Cannot set chat color.", Color.red);
                return false;
            }

        if (args.Length != 1) throw new ArgumentException("No arguments given!");

        if (args[0].ToLower() == "clear")
            return true;

        string pattern = "^#([0-9A-Fa-f]{6})$";
        Regex regex = new Regex(pattern);

        if(!regex.IsMatch(args[0])) {
            throw new ArgumentException("Invalid color format! Use hex format like #RRGGBB or use /chatcolor clear to reset.");
        }

        return true;
    }

    internal static bool listMods(Caller caller, string[] args) {
        if (Plugin.enableListingMods?.Value ?? false == false){
            NotifyCaller(caller, "Listing mods is disabled on this server.", Color.red);
            return true;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Loaded mods:");

        foreach (var plugin in Chainloader.PluginInfos) {
            sb.AppendLine($"{plugin.Value.Metadata.Name} - {plugin.Value.Metadata.Version}");
        }

        string pluginList = sb.ToString().TrimEnd();

        NotifyCaller(caller, pluginList);
        return true;
    }
}
