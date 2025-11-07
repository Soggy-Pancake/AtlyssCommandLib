using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using static AtlyssCommandLib.API.Utils;
using HarmonyLib;
using UnityEngine;

namespace AtlyssCommandLib.API;

/// <summary>
/// Delegate for command callbacks.
/// </summary>
/// <param name="caller"></param>
/// <param name="args"></param>
/// <returns></returns>
public delegate bool CommandCallback(Caller caller, string[] args);

/// <summary>
/// Provides a list of commands and sub-providers to the library.
/// </summary>
public class CommandProvider {

    internal string prefix;
    internal string helpHeader;

    internal bool hasClientCommands = false;
    internal bool hasServerCommands = false;
    internal bool hasConsoleCommands = false;

    /// <summary>
    /// The root CommandProvider.
    /// </summary>
    public static CommandProvider Root { get { return CommandManager.root; } }

    /// <summary>
    /// The parent CommandProvider of this provider.
    /// </summary>
    public CommandProvider? ParentProvider { get { return _parentProvider; } }
    internal CommandProvider? _parentProvider;

    internal struct Alias {
        public bool isProvider;
        public ModCommand command;
        public CommandProvider provider;
    }

    internal Dictionary<string, Alias> aliases = new Dictionary<string, Alias>(StringComparer.OrdinalIgnoreCase);
    internal Dictionary<string, ModCommand> commands = new Dictionary<string, ModCommand>(StringComparer.OrdinalIgnoreCase);

    // Should allow recursive searches so that there can be many nested categories of commands
    internal Dictionary<string, CommandProvider> childProviders = new Dictionary<string, CommandProvider>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a new CommandProvider.
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="header"></param>
    public CommandProvider(string prefix, string header) {
        this.prefix = prefix;
        helpHeader = header;
        _parentProvider = null;
    }

    /// <summary>
    /// Creates a new CommandProvider that registers with a parent provider.
    /// </summary>
    /// <param name="prefix"></param>
    /// <param name="header"></param>
    /// <param name="parentProvider"></param>
    public CommandProvider(string prefix, string header, CommandProvider? parentProvider = null) {
        this.prefix = prefix;
        helpHeader = header;

        _parentProvider = parentProvider ?? CommandManager.root;
        ParentProvider?.RegisterProvider(this);
    }

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
    public ModCommand? RegisterCommand(string command, string helpMessage, CommandCallback callback, bool clientSide = true, bool serverSide = false, bool console = false) {
        if (command.StartsWith('/'))
            command = command[1..];

        if (command.Contains(' ')) {
            Plugin.logger?.LogError($"Command '{command}' contains spaces! Not registering Command!");
            return null;
        }

        ModCommand cmd = new ModCommand(command, helpMessage, callback, clientSide, serverSide, console);
        RegisterCommand(cmd);
        return cmd;
    }

    /// <summary>
    /// Registers a command given a ModCommand object.
    /// </summary>
    /// <param name="cmd"></param>
    public void RegisterCommand(ModCommand cmd) {
        if (commands.ContainsKey(cmd.Command) || aliases.ContainsKey(cmd.Command)) {
            Plugin.logger?.LogError($"Failed to register Command '{cmd.Command}'! Command or alias with that name already exists!");
            return;
        }

        if (!(cmd.clientSideCommand || cmd.serverSideCommand || cmd.consoleCommand)) {
            Plugin.logger?.LogError($"Failed to register Command '{cmd.Command}'! Command has to either be a client, server, or console Command!");
            return;
        }

        if (cmd.consoleCommand && (cmd.clientSideCommand || cmd.serverSideCommand))
            Plugin.logger?.LogWarning($"A console Command '{prefix} {cmd.Command}' can be executed by clients! Make sure this is safe!");

        commands.Add(cmd.Command, cmd);

        // Apply server commands flag to all parents
        CommandProvider? parent = this;
        while (parent != null) {
            parent.hasClientCommands = parent.hasClientCommands || cmd.clientSideCommand;
            parent.hasServerCommands = parent.hasServerCommands || cmd.serverSideCommand;
            parent.hasConsoleCommands = parent.hasConsoleCommands || cmd.consoleCommand;
            parent = parent.ParentProvider;
        }
    }

    /// <summary>
    /// Registers a new sub-CommandProvider.
    /// </summary>
    /// <param name="provider"></param>
    public void RegisterProvider(CommandProvider provider) {
        if (childProviders.ContainsKey(provider.prefix) || aliases.ContainsKey(provider.prefix))
            return;
        childProviders.Add(provider.prefix, provider);
        provider._parentProvider = this;

        Plugin.logger?.LogInfo($"Registered Command command '{provider.prefix}' under '{this.prefix}'");
    }

    /// <summary>
    /// Registers a new alias for a command.
    /// </summary>
    /// <param name="alias"></param>
    /// <param name="command"></param>
    public void RegisterAlias(string alias, ModCommand? command) {
        if (command == null || commands.ContainsKey(alias) || aliases.ContainsKey(alias))
            return;
        aliases.Add(alias, new Alias { isProvider = false, command = command });
    }

    /// <summary>
    /// Registers multiple aliases for a command.
    /// </summary>
    /// <param name="aliases"></param>
    /// <param name="command"></param>
    public void RegisterAlias(string[] aliases, ModCommand? command) {
        if (command == null)
            return;
        foreach (string alias in aliases) {
            if (commands.ContainsKey(alias) || this.aliases.ContainsKey(alias))
                return;
            RegisterAlias(alias, command);
        }
    }

    /// <summary>
    /// Registers a new alias for a CommandProvider.
    /// </summary>
    /// <param name="alias"></param>
    /// <param name="provider"></param>
    public void RegisterAlias(string alias, CommandProvider? provider) {
        if (provider == null || commands.ContainsKey(alias) || aliases.ContainsKey(alias))
            return;
        aliases.Add(alias, new Alias { isProvider = true, provider = provider });
    }

    /// <summary>
    /// Registers multiple aliases for a CommandProvider.
    /// </summary>
    /// <param name="aliases"></param>
    /// <param name="provider"></param>
    public void RegisterAlias(string[] aliases, CommandProvider? provider) {
        if (provider == null)
            return;
        foreach (string alias in aliases) {
            if (commands.ContainsKey(alias) || this.aliases.ContainsKey(alias))
                return;
            RegisterAlias(alias, provider);
        }
    }

    internal bool recieveCommand(Caller caller, string command, string[] args) {
        // Return true if message was handled here

        if (command.StartsWith('/'))
            command = command[1..];
        caller.cmdPrefix = command;

        bool amServer = Player._mainPlayer.NC()?.Network_isHostPlayer ?? false;

        if (commands.ContainsKey(command) || (aliases.ContainsKey(command) && !aliases[command].isProvider)) {
            try {
                ModCommand targetCmd;
                if (commands.ContainsKey(command))
                    targetCmd = commands[command];
                else
                    targetCmd = aliases[command].command;

                return CommandManager.execCommand(targetCmd, caller, args);

            } catch (ArgumentException e) {
                if (!string.IsNullOrEmpty(e.Message) && string.IsNullOrEmpty(e.ParamName)) {
                    NotifyCaller(caller, $"Recieved invalid arguments for command '{command} {string.Join(" ", args)}' Reason: " + e.Message, Color.yellow);
                } else if (!string.IsNullOrEmpty(e.ParamName) && !string.IsNullOrEmpty(e.Message)) {
                    NotifyCaller(caller, $"'{e.ParamName}' is invalid! Reason: {e.Message}");
                } else
                    Plugin.logger?.LogError($"Recieved invalid arguments for command '{command} {string.Join(" ", args)}' Error: " + e);

                commands[command].printHelp(caller);
                return true;
            } catch (Exception e) {
                Plugin.logger?.LogError($"Error executing command '{command} {string.Join(" ", args)}' Error: " + e);
                commands[command].printHelp(caller);
                return true;
            }
        }

        if (childProviders.ContainsKey(command) || (aliases.ContainsKey(command) && aliases[command].isProvider)) {
            CommandProvider targetProvider;
            if (aliases.ContainsKey(command))
                targetProvider = aliases[command].provider;
            else
                targetProvider = childProviders[command];

            string cmd = args.Length > 0 ? args[0] : "";

            targetProvider.recieveCommand(caller, cmd, args.Length > 1 ? args[1..] : []);
        } else {
            PrintHelp(caller, command);
        }

        return true;
    }

    /// <summary>
    /// Gets the help header for this CommandProvider.
    /// </summary>
    /// <returns></returns>
    public string getHelpMessage() {
        return helpHeader;
    }

    /// <summary>
    /// Prints the help message for this CommandProvider to the caller.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="cmd"></param>
    public virtual void PrintHelp(Caller caller, string cmd) {
        string msg = "";

        if(helpHeader != "")
            msg += '\n' + helpHeader + "\n\n";

        msg += buildHelpMessage(caller, this);

        List<string> stack = new();
        CommandProvider? p = this;
        while (p?.ParentProvider != null) {
            stack.Add(p.prefix);
            p = p.ParentProvider;
        }

        string list = "";
        if (stack.Count > 0) {
            stack.Reverse();
            list = string.Join(' ', stack);
            list += ' ';
        }

        msg += $"\nCommand '{cmd}' not found! Use /help {list}to list available comands";
        NotifyCaller(caller, msg);
    }
}
