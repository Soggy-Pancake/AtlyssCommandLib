using System;
using HarmonyLib;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine.UI;
using AtlyssCommandLib.API;
using CodeTalker.Networking;
using UnityEngine;
using CodeTalker;
using static AtlyssCommandLib.API.CommandProvider;
using static CodeTalker.Compressors;
using static AtlyssCommandLib.API.Utils;

namespace AtlyssCommandLib;
internal static class Patches {
    public static bool blockMsg = false; // Stored for postfix patches

    public static string[] commandSplit(string message) => Regex.Matches(message, @"[\""].+?[\""]|[^ ]+")
        .Cast<Match>()
        .Select(m => m.Value.Trim('"'))
        .Where(m => !string.IsNullOrWhiteSpace(m))
        .ToArray();
    
    public static bool getValidCommand(string message, out string[] args)
    {
        args = [];
            
        var match = Regex.Match(message, @"<color=.*>(.*)<\/color>");

        if (match.Success)
            message = match.Groups[1].Value;
        
        if (!message.StartsWith('/') || message.StartsWith("//") || message.Length == 0)
            return false;

        message = message[1..];

        args = commandSplit(message);

        return args.Length > 0;
    }

    [HarmonyPrefix]
    [HarmonyPriority(int.MaxValue)]
    [HarmonyPatch(typeof(ChatBehaviour), "Cmd_SendChatMessage")]
    internal static void Client_SendChatMessage(ref ChatBehaviour __instance, ref bool __runOriginal, string _message) {
        //Plugin.logger?.LogDebug("Send chat message!");
        if (!getValidCommand(_message, out var args))
            return;

        Caller caller = new Caller(args[0], Player._mainPlayer);

        var (provider, resolvedCommand, rest) = CommandManager.root.resolveCommand(args);
        
        if (resolvedCommand != null)
        {
            var options = resolvedCommand.options;

            if (options.chatCommand == ChatCommandType.ClientSide || (options.chatCommand == ChatCommandType.HostOnly && caller.isHost))
            {
                // Normally vanilla commands are intercepted and don't reach this method
                Plugin.logger?.LogDebug($"Procesing command {_message} in {nameof(Client_SendChatMessage)}");
                CommandManager.execCommand(resolvedCommand, caller, rest);
                blockMsg = true;
                __runOriginal = false;
                return;
            }
            else if (options.chatCommand == ChatCommandType.ServerSide)
            {
                Plugin.logger?.LogDebug($"Sending command {_message} in {nameof(Client_SendChatMessage)} to the server");
                __runOriginal = true;
                return; // Send this to the server if the handler returned a success status
            }
            else
            {
                Plugin.logger?.LogDebug($"Caller {caller} is not allowed to use command {resolvedCommand} in {nameof(Client_SendChatMessage)}");
                blockMsg = true;
                __runOriginal = false;
                return;
            }
        }

        if (provider == Root) {
            if (!caller.isHost && CommandManager.serverCommands.ContainsKey(args[0])) {
                Plugin.logger?.LogDebug($"Sending known server command {_message} in {nameof(Client_SendChatMessage)} to the server");
                __runOriginal = true; // This is available according to the server, so send it
                return;
            }

            if ((ModConfig.sendFailedCommands?.Value ?? false))
            {
                Plugin.logger?.LogDebug($"Allowing failed root level command {_message} to go through in {nameof(Client_SendChatMessage)}!");
                __runOriginal = true; // Assume it's an unknown external server side command and send it anyway
                return;
            }
        }

        NotifyCaller(caller, provider.GetNotFoundHelpCommand(args[^1]));
        blockMsg = true;
        __runOriginal = false; // Do not send this command
    }

    [HarmonyPrefix]
    [HarmonyPriority(int.MinValue)]
    [HarmonyPatch(typeof(ChatBehaviour), "Cmd_SendChatMessage")]
    internal static void Client_BlockChatMessage(ref ChatBehaviour __instance, ref string _message, ref bool __runOriginal) {
        // THE GREAT FILTER
        if (blockMsg) 
            __runOriginal = false; // Force block command from sending
        
        blockMsg = false;
    }
    
    [HarmonyPrefix]
    [HarmonyPriority(int.MinValue)]
    [HarmonyPatch(typeof(ChatBehaviour), "Rpc_RecieveChatMessage")]
    internal static void Server_RecieveChatMessage(ref ChatBehaviour __instance, ref bool __runOriginal, string message) {
        //Plugin.logger?.LogDebug("Recieve chat message!");

        if (__instance == null || !getValidCommand(message, out var args))
            return;

        Caller caller = new(args[0], __instance.gameObject.GetComponent<Player>());
        
        var (provider, resolvedCommand, rest) = CommandManager.root.resolveCommand(args);
        
        // Skip sending the message to other clients *only if* we parsed the command and it's not a vanilla / compatibility command
        if (resolvedCommand != null)
        {
            var options = resolvedCommand.options;

            if (options.chatCommand == ChatCommandType.ServerSide)
            {
                Plugin.logger?.LogDebug($"Procesing command {message} in {nameof(Server_RecieveChatMessage)}");
                CommandManager.execCommand(resolvedCommand, caller, rest);
                __runOriginal = options.mustRunVanillaCode;
            }
            else
            {
                //Plugin.logger?.LogDebug($"Caller {caller} is not allowed to use command {resolvedCommand} in {nameof(Server_RecieveChatMessage)}");
                __runOriginal = false;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPriority(int.MinValue)]
    [HarmonyPatch(typeof(HostConsole), "Send_ServerMessage")]
    internal static void Console_RecieveCommand(ref HostConsole __instance, string _message, ref bool __runOriginal) {
        if (!getValidCommand(_message, out var args))
            return;

        // could be nice to put the player in here if we want to track oped player commands

        Caller caller = new(args[0], null, console: true);

        var (_, resolvedCommand, rest) = CommandManager.root.resolveCommand(args);
        
        // Skip running vanilla code *only if* we parsed it and it's not a vanilla / compatibility command
        if (resolvedCommand != null)
        {
            var options = resolvedCommand.options;
            if (options.consoleCmd)
            {
                Plugin.logger?.LogDebug($"Procesing command {_message} in {nameof(Console_RecieveCommand)}");
                CommandManager.execCommand(resolvedCommand, caller, rest);
                __runOriginal = options.mustRunVanillaCode;
            }
            else
            {
                Plugin.logger?.LogDebug($"Caller {caller} is not allowed to use command {resolvedCommand} in {nameof(Console_RecieveCommand)}");
                __runOriginal = false;
            }
        }
        __instance._consoleInputField.text = "";
    }

    [HarmonyPatch(typeof(PlayerMove), "Start")]
    [HarmonyPostfix]
    internal static void SendCommandList(PlayerMove __instance) {
        if (!Player._mainPlayer.NC()?._isHostPlayer ?? true)
            return;

        Player target = __instance.GetComponent<Player>();
        if (target != null && target != Player._mainPlayer) {
            Plugin.logger?.LogDebug("sending server command list!");
            CodeTalkerNetwork.SendNetworkPacket(target, new ServerCommandPkt(), Compressors.CompressionType.Brotli);
        }
    }

    [HarmonyPatch(typeof(AtlyssNetworkManager), "OnStartServer")]
    [HarmonyPostfix]
    internal static void StartHosting() {
        CommandManager.updateServerCommands();
    }
}
