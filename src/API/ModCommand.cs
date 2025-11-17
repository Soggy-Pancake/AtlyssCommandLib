using System;
using System.Collections.Generic;
using System.Text;
using AtlyssCommandLib.src.API;
using static AtlyssCommandLib.API.Utils;

namespace AtlyssCommandLib.API; 

/// <summary>
/// A Command object that contains the callback and help message for a command.
/// </summary>
public class ModCommand {

    internal string Command;
    string HelpMessage = "";
    string DetailedHelpMessage = "";

    internal CommandCallback Callback;
    internal CommandOptions options;

    /// <summary>
    /// Creates a new ModCommand.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="helpMessage"></param>
    /// <param name="callback"></param>
    /// <param name="clientSide"></param>
    /// <param name="serverSide"></param>
    /// <param name="console"></param>
    public ModCommand(string command, string helpMessage, CommandCallback callback, bool clientSide, bool serverSide, bool console) {
        Command = command;
        HelpMessage = helpMessage.Trim();
        Callback = callback;
        options = new CommandOptions(clientSide, serverSide, console);
    }

    /// <summary>
    /// Creates a new ModCommand with detailed help.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="helpMessage"></param>
    /// <param name="detailedHelp"></param>
    /// <param name="callback"></param>
    /// <param name="clientSide"></param>
    /// <param name="serverSide"></param>
    /// <param name="console"></param>
    public ModCommand(string command, string helpMessage, string detailedHelp, CommandCallback callback, bool clientSide, bool serverSide, bool console) {
        Command = command;
        HelpMessage = helpMessage.Trim();
        DetailedHelpMessage = detailedHelp.Trim();
        Callback = callback;
        options = new CommandOptions(clientSide, serverSide, console);
    }

    /// <summary>
    /// Creates a new ModCommand with CommandOptions.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="helpMessage"></param>
    /// <param name="detailedHelp"></param>
    /// <param name="callback"></param>
    /// <param name="options"></param>
    public ModCommand(string command, string helpMessage, CommandCallback callback, CommandOptions options) {
        Command = command;
        HelpMessage = helpMessage.Trim();
        Callback = callback;
        this.options = options;
    }

    /// <summary>
    /// Creates a new ModCommand with CommandOptions.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="helpMessage"></param>
    /// <param name="detailedHelp"></param>
    /// <param name="callback"></param>
    /// <param name="options"></param>
    public ModCommand(string command, string helpMessage, string detailedHelp, CommandCallback callback, CommandOptions options) {
        Command = command;
        HelpMessage = helpMessage.Trim();
        DetailedHelpMessage = detailedHelp.Trim();
        Callback = callback;
        this.options = options;
    }


    /// <summary>
    /// Send help message to caller. Only called when command fails.
    /// </summary>
    /// <param name="caller"></param>
    public void printHelp(Caller caller) {
        string message = $"/{Command} - {HelpMessage} {DetailedHelpMessage}";
        NotifyCaller(caller, message);
    }

    /// <summary>
    /// Get the help message for this command.
    /// </summary>
    /// <returns></returns>
    public string getHelpMessage() { 
        return HelpMessage;
    }

    public string getDetailedHelpMessage() {
        return DetailedHelpMessage;
    }
}
