using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using AtlyssCommandLib.API;
using static AtlyssCommandLib.API.Utils;
using CodeTalker.Networking;
using BepInEx.Configuration;

namespace AtlyssCommandLib;

[BepInDependency("CodeTalker")]
[BepInDependency("EasySettings", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("StuntedRaccoon.CustomChatColors", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
internal class Plugin : BaseUnityPlugin {

    internal static ManualLogSource? logger;
    internal static Harmony? harmony;

    internal static bool chatColorsInstalled = false;

    private void Awake() {
        logger = Logger;
        logger.LogInfo($"Plugin {PluginInfo.NAME} is loaded!");

        if (Application.version != PluginInfo.GAMEVER) {
            logger.LogWarning($"[VERSION MISMATCH] This version of AtlyssCommandLib is made for {PluginInfo.GAMEVER}, you are running {Application.version}.");
        }

        harmony = new Harmony(PluginInfo.GUID);
        harmony.PatchAll(typeof(Patches));

        ModConfig.init(Config);

        chatColorsInstalled = Chainloader.PluginInfos.ContainsKey("StuntedRaccoon.CustomChatColors");
        logger.LogInfo($"Chatcolors {(chatColorsInstalled ? "is" : "isn't")} installed!");

        CodeTalkerNetwork.RegisterBinaryListener<ServerCommandPkt>(CommandManager.updateServerCommands);

        RegisterCommand("help", "Shows this help message", BuiltInCmds.Help, new(ChatCommandType.ClientSide, consoleCmd: true));
        RegisterCommand("mods", "List server's installed mods!", BuiltInCmds.listMods, new(ChatCommandType.ServerSide));

        registerVanillaCommands();
        addCommandCompatibility();

        if (ModConfig.enableTestCommands?.Value ?? false)
            registerTestCommands();
    }

    private void registerVanillaCommands() {
        // Client commands (Cmd_SendChatMessage)
        CommandOptions clientSide = new(ChatCommandType.ClientSide) { mustRunVanillaCode = true };
        RegisterCommand("emotes", "Lists all available emotes", vanillaCommandDummyCallback, clientSide);
        
        // Server commands (Server_RecieveChatMessage)
        CommandOptions serverSide = new(ChatCommandType.ServerSide) { mustRunVanillaCode = true };
        RegisterCommand("afk", "Go afk", vanillaCommandDummyCallback, serverSide);

        // Console commands (Send_ServerMessage)
        CommandOptions vanillaConsoleOpt = new(ChatCommandType.None, consoleCmd: true)  { mustRunVanillaCode = true };
        RegisterCommand("shutdown", "Shuts down the server with optional countdown.", vanillaCommandDummyCallback, vanillaConsoleOpt);
        RegisterCommand("cancelsd", "Cancel shutting down the server.", vanillaCommandDummyCallback, vanillaConsoleOpt);
        RegisterCommand("starthost", "initalizes the server. Server instance must be shut down to initalize.", vanillaCommandDummyCallback, vanillaConsoleOpt);
        RegisterCommand("kick", "kicks a connected client. Must have a output of the connection ID number. (ex: /kick 2)", vanillaCommandDummyCallback, vanillaConsoleOpt);
        RegisterCommand("ban", "same as kick, but also bans the client's address from connecting to the server. (ex: /ban 2)", vanillaCommandDummyCallback, vanillaConsoleOpt);
        RegisterCommand("clearbanlist", "clears the list of bans saved in your host settings profile.", vanillaCommandDummyCallback, vanillaConsoleOpt);
        RegisterCommand("clients", "displays all clients that are connected to the server with Connnection IDs.", vanillaCommandDummyCallback, vanillaConsoleOpt);
        RegisterCommand("maplist", "displays all loaded map instances on the server.", vanillaCommandDummyCallback, vanillaConsoleOpt);
        RegisterCommand("savelog", "clears console log.", vanillaCommandDummyCallback, vanillaConsoleOpt);
        
        // Client + console commands
        CommandOptions serverConsoleOpt = new(ChatCommandType.ClientSide, consoleCmd: true) { mustRunVanillaCode = true };
        RegisterCommand("clear", "clears chat or console log.", vanillaCommandDummyCallback, serverConsoleOpt);
    }
    
    private void addCommandCompatibility()
    {
        CommandOptions chatColorOpt = new(ChatCommandType.ServerSide);
        RegisterCommand("chatcolor", "Set your chat color using a hex code! /chatcolor #[color]. HASHTAG REQUIRED", BuiltInCmds.ChatColorProtector, chatColorOpt);

        // Add others if desired
    }

    private bool vanillaCommandDummyCallback(Caller caller, string[] args)
    {
        logger?.LogDebug($"Vanilla command '{caller.cmdPrefix}' called!");
        return true;
    }
    
    private void registerTestCommands() {
        RegisterCommand("test-cs", "Run a clientside command", BuiltInCmds.testClientSide, new(ChatCommandType.ClientSide));
        RegisterCommand("test-ss", "Run a serverside command", BuiltInCmds.testServerSide, new(ChatCommandType.ServerSide));
        RegisterCommand("test-host", "Run a host only command", BuiltInCmds.testHostOnlyCmd, new(ChatCommandType.HostOnly));
        RegisterCommand("test-cons", "Run a console command", BuiltInCmds.testConsoleCmd, new(ChatCommandType.None, consoleCmd: true));

        // Add others if desired
    }
}
