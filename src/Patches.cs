using System;
using HarmonyLib;
using System.Reflection;
using static AtlyssCommandLib.API.CommandProvider;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine.UI;
using AtlyssCommandLib.API;
using CodeTalker.Networking;
using static AtlyssCommandLib.API.Utils;

namespace AtlyssCommandLib;
internal static class Patches {

    static ScriptableEmoteList? emoteList;
    static InputField? consoleInputField;
    static bool blockMsg = false; // Stored for postfix patches
    public static string blockReason = "";

    public static string[] commandSplit(string message) => Regex.Matches(message, @"[\""].+?[\""]|[^ ]+")
                     .Cast<Match>()
                     .Select(m => m.Value.Trim('"'))
                     .Where(m => !string.IsNullOrWhiteSpace(m))
                     .ToArray();

    [HarmonyPrefix]
    [HarmonyPriority(int.MaxValue)]
    [HarmonyPatch(typeof(ChatBehaviour), "Cmd_SendChatMessage")]
    internal static bool Client_SendChatMessage(ref ChatBehaviour __instance, ref bool __runOriginal, ref string _message) {
        Plugin.logger?.LogInfo("Send chat message!");

        if (!_message.StartsWith('/') || _message.StartsWith("//") || _message.Length == 0)
            return true;

        bool isEmote(string _message) {
            if (emoteList == null) return false;

            foreach (var emote in emoteList._emoteCommandList)
                if (_message == emote._emoteChatCommand)
                    return true;

            return false;
        }

        if (emoteList == null) {
            var listField = __instance.GetType().GetField("_scriptableEmoteList", BindingFlags.NonPublic | BindingFlags.Instance);
            emoteList = (ScriptableEmoteList)listField.GetValue(__instance);
        }

        if(_message == "/afk" || isEmote(_message))
            return true;

        if (_message.StartsWith("/hb") && Plugin.homebreweryInstalled) // hb compatibility
            return true;

        if (_message.StartsWith("/")) {
            var args = commandSplit(_message);

            Caller caller = new Caller { player = Player._mainPlayer };
            blockMsg = CommandManager.root.recieveCommand(caller, args[0], args.Length > 1 ? args[1..] : []);
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPriority(int.MinValue)]
    [HarmonyPatch(typeof(ChatBehaviour), "Cmd_SendChatMessage")]
    internal static bool Client_BlockChatMessage(ref ChatBehaviour __instance, ref string _message, ref bool __runOriginal) {
        // THE GREAT FILTER
        if (blockMsg) {
            __runOriginal = false;
            blockMsg = false;

            if (blockReason != "") { 
                NotifyCaller(new Caller { player = Player._mainPlayer }, blockReason);
                blockReason = "";
            }
            //CommandManager.HandleCommand(_message);
        }

            return false;
        }
        return true;
    }


    [HarmonyPrefix]
    [HarmonyPriority(int.MinValue)]
    [HarmonyPatch(typeof(ChatBehaviour), "Rpc_RecieveChatMessage")]
    internal static void Server_RecieveChatMessage(ref ChatBehaviour __instance, ref bool __runOriginal, ref string message) {
        Plugin.logger?.LogInfo("Recieve chat message!");

        if (!message.StartsWith('/') || message.StartsWith("//") || message.Length == 0 || __instance == null)
            return;


        string cmd = message.Split()[0][1..];
        var args = commandSplit(message);

        Caller caller = new(cmd, __instance.gameObject.GetComponent<Player>());

        __runOriginal = !CommandManager.root.recieveCommand(caller, args[0], args.Length > 1 ? args[1..] : []);
    }

    [HarmonyPrefix]
    [HarmonyPriority(int.MinValue)]
    [HarmonyPatch(typeof(HostConsole), "Send_ServerMessage")]
    internal static void Console_RecieveCommand(ref HostConsole __instance, ref string _message, ref bool __runOriginal) {
        if (!_message.StartsWith('/') || _message.Length == 0)
            return;

        // could be nice to put the player in here if we want to track oped player commands

        string cmd = _message.Split()[0][1..];
        var args = commandSplit(_message);

        Caller caller = new(cmd, null, console: true);

        __runOriginal = !CommandManager.root.recieveCommand(caller, args[0], args.Length > 1 ? args[1..] : []);

        if(consoleInputField == null) {
            var inputFieldField = typeof(HostConsole).GetField("_consoleInputField", BindingFlags.NonPublic | BindingFlags.Instance);
            consoleInputField = (InputField)inputFieldField.GetValue(__instance);
        }
        consoleInputField.text = String.Empty;
    }

    [HarmonyPatch(typeof(PlayerMove), "Start")]
    [HarmonyPrefix]
    internal static void SendCommandList() {
        if(Player._mainPlayer.NC()?.Network_isHostPlayer ?? false)
            return;
        Plugin.logger?.LogWarning("SENDING SERVER COMMAND LIST!");
        CodeTalkerNetwork.SendNetworkPacket(new ServerCommandPkt());
    }

    [HarmonyPatch(typeof(AtlyssNetworkManager), "OnStartServer")]
    [HarmonyPostfix]
    internal static void StartHosting() {
        CommandManager.updateServerCommands();
    }
}
