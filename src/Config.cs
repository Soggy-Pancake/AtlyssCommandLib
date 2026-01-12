using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Nessie.ATLYSS.EasySettings;

namespace AtlyssCommandLib;

internal class ModConfig {

    public static ConfigFile? configFile;
    public static ConfigEntry<bool>? enableListingMods;
    public static ConfigEntry<bool>? enableTestCommands;
    public static ConfigEntry<bool>? sendFailedCommands; // Allows sending commands that failed to parse client side to server anyway

    public static void init(ConfigFile config) {

        configFile = config;
        enableListingMods = config.Bind("General", "EnableModListing", true, "Enable the /mods command to list server mods.");
        enableTestCommands = config.Bind("General", "EnableTestCommands", true, "Enable commands for testing CommandLib.");
        sendFailedCommands = config.Bind("General", "sendFailedCommandsToServer", true, "If a command fails to parse on the client side, still send it to the server. Useful for commands that only run server side. CommandLib will no longer print the error help message! The server will still block commands from properly entering chat, but if the server is vanilla it will enter chat!");

        if (Chainloader.PluginInfos.ContainsKey("EasySettings")) {
            try {
                Plugin.logger?.LogInfo("EasySettings detected, adding settings tab.");
                easySettings();
            } catch { }
        }

    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void easySettings() {
        Settings.OnApplySettings.AddListener(() => { configFile?.Save(); });

        Settings.OnInitialized.AddListener(() => {
            SettingsTab tab = Settings.ModTab;

            tab.AddHeader("Atlyss Command Lib Settings");
            tab.AddToggle(enableListingMods);
            tab.AddToggle(sendFailedCommands);
        });
    }
}
