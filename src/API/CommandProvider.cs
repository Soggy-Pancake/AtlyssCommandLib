using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using static AtlyssCommandLib.API.Utils;
using HarmonyLib;
using UnityEngine;

namespace AtlyssCommandLib.API;

/// <summary>
/// Delegate for command callbacks.
/// If it returns true, the command is considered to have been successful.
/// If it returns false, the command is considered to have failed, and a help message will be printed.
/// For server side commands, when the call comes from the send chat method, the return value is instead
/// used to determine if the command should be sent to the server.
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
    internal bool hasHostOnlyCommands = false;

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
    /// Registers a command with command options.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="helpMessage"></param>
    /// <param name="callback"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public ModCommand? RegisterCommand(string command, string helpMessage, CommandCallback callback, CommandOptions? options = null) {
        if (command.StartsWith('/'))
            command = command[1..];

        if (command.Contains(' ')) {
            Plugin.logger?.LogError($"Command '{command}' contains spaces! Not registering Command!");
            return null;
        }

        ModCommand cmd = new ModCommand(command, helpMessage, callback, options ?? new());
        RegisterCommand(cmd);
        return cmd;
    }

    /// <summary>
    /// Registers a command with command options.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="helpMessage"></param>
    /// <param name="detailedHelpMessage"></param>
    /// <param name="callback"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public ModCommand? RegisterCommand(string command, string helpMessage, string detailedHelpMessage, CommandCallback callback, CommandOptions? options = null) {
        if (command.StartsWith('/'))
            command = command[1..];

        if (command.Contains(' ')) {
            Plugin.logger?.LogError($"Command '{command}' contains spaces! Not registering Command!");
            return null;
        }

        ModCommand cmd = new ModCommand(command, helpMessage, detailedHelpMessage, callback, options ?? new());
        RegisterCommand(cmd);
        return cmd;
    }

    /// <summary>
    /// Registers a command given a ModCommand object.
    /// </summary>
    /// <param name="cmd"></param>
    public void RegisterCommand(ModCommand cmd) {

        CommandOptions options = cmd.options;
        if (commands.ContainsKey(cmd.Command) || aliases.ContainsKey(cmd.Command)) {
            Plugin.logger?.LogError($"Failed to register Command '{cmd.Command}'! Command or alias with that name already exists!");
            return;
        }

        if (!Enum.IsDefined(typeof(ChatCommandType), options.chatCommand)) {
            Plugin.logger?.LogError($"Failed to register Command '{cmd.Command}'! Command has to have a valid chat command type!");
            return;
        }
        
        commands.Add(cmd.Command, cmd);

        // Apply server commands flag to all parents
        CommandProvider? parent = this;
        CommandOptions cmdOptions = cmd.options;
        while (parent != null) {
            parent.hasClientCommands = parent.hasClientCommands || cmdOptions.chatCommand == ChatCommandType.ClientSide;
            parent.hasServerCommands = parent.hasServerCommands || cmdOptions.chatCommand == ChatCommandType.ServerSide;
            parent.hasConsoleCommands = parent.hasConsoleCommands || cmdOptions.consoleCmd;
            parent.hasHostOnlyCommands = parent.hasHostOnlyCommands || cmdOptions.chatCommand == ChatCommandType.HostOnly;
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

    internal (CommandProvider provider, ModCommand? resolvedCommand, string[] rest) resolveCommand(string[] args)
    {
        if (args.Length == 0)
            return (this, null, []);

        var command = args[0];
        var rest = args.Length > 1 ? args[1..] : [];
        
        if (commands.TryGetValue(command, out ModCommand resolvedCommand))
            return (this, resolvedCommand, rest);

        if (aliases.TryGetValue(command, out Alias resolvedAlias) && !resolvedAlias.isProvider)
            return (this, resolvedAlias.command, rest);

        if (childProviders.TryGetValue(command, out CommandProvider childProvider))
            return childProvider.resolveCommand(args[1..]);

        if (aliases.TryGetValue(command, out Alias resolvedChildAlias) && resolvedChildAlias.isProvider)
            return resolvedChildAlias.provider.resolveCommand(args[1..]);

        return (this, null, []);
    }

    internal string GetNotFoundHelpCommand(string command)
    {
        List<string> stack = new();
        CommandProvider? p = this;
        while (p.ParentProvider != null) {
            stack.Add(p.prefix);
            p = p.ParentProvider;
        }

        string list = "";
        if (stack.Count > 0) {
            stack.Reverse();
            list = string.Join(' ', stack);
            list += ' ';
        }

        return $"\nCommand '{command}' not found! Use /help {list}to list available comands";
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
