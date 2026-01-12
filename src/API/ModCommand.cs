using System;
using System.Collections.Generic;
using System.Text;
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
    /// Creates a new ModCommand with CommandOptions.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="helpMessage"></param>
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
    /// Send help message to caller. Usually only called when command fails.
    /// </summary>
    /// <param name="caller"></param>
    public void printHelp(Caller caller) {
        string message = $"/{Command} - {HelpMessage} {DetailedHelpMessage}";
        NotifyCaller(caller, message);
    }

    /// <summary>
    /// returns help message for this command.
    /// </summary>
    /// <returns></returns>
    public string getHelpMessage() { 
        return HelpMessage;
    }

    /// <summary>
    /// returns detailed help message for this command.
    /// </summary>
    /// <returns></returns>
    public string getDetailedHelpMessage() {
        return DetailedHelpMessage;
    }

    public override string ToString()
    {
        return $"{Command} {options}";
    }
}
