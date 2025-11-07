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
[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
internal class Plugin : BaseUnityPlugin {

    internal static ManualLogSource? logger;
    internal static Harmony? harmony;

    internal static HostConsole? hostConsole;
    internal static ConsoleCommandManager? consoleCommandManager;

    internal static CommandProvider? serverCmds;
    internal static CommandProvider? clientCmds;

    internal static bool chatColorsInstalled = false;
    internal static ConfigEntry<bool>? enableListingMods;

    void Awake() {
        logger = Logger;
        logger.LogInfo($"Plugin {PluginInfo.NAME} is loaded!");
        harmony = new Harmony(PluginInfo.GUID);
        harmony.PatchAll(typeof(Patches));

        enableListingMods = Config.Bind("General", "EnableModListing", true, "Enable the /mods command to list server mods.");

        chatColorsInstalled = Chainloader.PluginInfos.ContainsKey("StuntedRaccoon.CustomChatColors");
        logger.LogInfo($"Chatcolors {(chatColorsInstalled ? "is" : "isn't")} installed!");

        CodeTalkerNetwork.RegisterBinaryListener<ServerCommandPkt>(CommandManager.updateServerCommands);

        RegisterCommand("help", "Shows this help message", BuiltInCmds.Help, serverSide: true);
        // RegisterCommand("test", "Test command", testTopLevel);
        if(enableListingMods.Value)
            RegisterCommand("mods", "List server's installed mods!", BuiltInCmds.listMods, clientSide: false, serverSide: true);

        registerVanillaCommands();
        addCommandCompatibility();
        /* Custom command provider test! You can provide an optional parent provider here -------------V
        serverCmds = new CommandProvider("server", "Test commands that run on the server side only", CommandProvider.Root);
        ModCommand? cmd = serverCmds.RegisterCommand("test2", "A test command thats server only", testCmd, clientSide: false, serverSide: true);
        serverCmds.RegisterAlias(["idk", "testAlias", "fuck", "me"], cmd);

        clientCmds = new CommandProvider("clientStuffs", "Test client side only nested");
        clientCmds.RegisterCommand("test8", "A test command thats client only", testCmd); */
    }

    internal static void getHostConsole() {
        if (hostConsole != null)
            return;

        hostConsole = FindFirstObjectByType<HostConsole>();

        if (hostConsole == null) {
            logger?.LogError("Failed to find HostConsole instance!");
            return;
        }

        var consoleCommandManagerField = typeof(HostConsole).GetField("_cmdManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        consoleCommandManager = (ConsoleCommandManager)consoleCommandManagerField.GetValue(hostConsole);
    }

    void registerVanillaCommands() {
        // Normal client commands
        RegisterCommand("emotes", "Lists all available emotes", compaitibilityCommand, serverSide: true);
        RegisterCommand("afk", "Go afk", compaitibilityCommand, serverSide: true);

        // Console commands
        RegisterCommand("shutdown", "Shuts down the server with optional countdown.", vanillaServerCommand, clientSide: false, console: true);
        RegisterCommand("cancelsd", "Cancel shutting down the server.", vanillaServerCommand, clientSide: false, console: true);
        RegisterCommand("starthost", "initalizes the server. Server instance must be shut down to initalize.", vanillaServerCommand, clientSide: false, console: true);
        RegisterCommand("kick", "kicks a connected client. Must have a output of the connection ID number. (ex: /kick 2)", vanillaServerCommand, clientSide: false, console: true);
        RegisterCommand("ban", "same as kick, but also bans the client's address from connecting to the server. (ex: /ban 2)", vanillaServerCommand, clientSide: false, console: true);
        RegisterCommand("clearbanlist", "clears the list of bans saved in your host settings profile.", vanillaServerCommand, clientSide: false, console: true);
        RegisterCommand("clients", "displays all clients that are connected to the server with Connnection IDs.", vanillaServerCommand, clientSide: false, console: true);
        RegisterCommand("maplist", "displays all loaded map instances on the server.", vanillaServerCommand, clientSide: false, console: true);
        RegisterCommand("savelog", "clears console log.", vanillaServerCommand, clientSide: false, console: true);
        RegisterCommand("clear", "clears console log.", vanillaServerCommand, clientSide: false, console: true);
    }

    void addCommandCompatibility() {
        RegisterCommand("chatcolor", "Set your chat color using a hex code! /chatcolor #[color]. HASHTAG REQUIRED", BuiltInCmds.ChatColorProtector, serverSide: true);

        // Add others?
    }

    bool vanillaServerCommand(Caller caller, string[] args) {
        if (!caller.isConsole) return true;
        logger?.LogInfo($"Vanilla server command '{caller.cmdPrefix}' called");
        return false;
    }

    // use as delegate for commands that are handled by server side plugins that arent registered with AtlyssCommandLib
    bool compaitibilityCommand(Caller caller, string[] args) {
        return false;
    }

    bool testCmd(Caller caller, string[] args) {
        NotifyCaller(caller, "Test command executed!");
        foreach (var arg in args)
            logger?.LogInfo("Arg: " + '"' + arg + '"');
        return true;
    }

    bool testTopLevel(Caller caller, string[] args) {
        logger?.LogInfo("Yo?");

        foreach (var arg in args)
            logger?.LogInfo("Arg: " + '"' + arg + '"');

        return true;
    }
}
