using System;
using System.Collections.Generic;
using System.Text;
using static AtlyssCommandLib.API.Utils;

namespace AtlyssCommandLib.API {

    /// <summary>
    /// A Command object that contains the callback and help message for a command.
    /// </summary>
    public class ModCommand {

        internal string Command;
        string HelpMessage;

        internal CommandCallback Callback;
        internal bool clientSideCommand;
        internal bool serverSideCommand;
        internal bool consoleCommand;

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
            HelpMessage = helpMessage;
            Callback = callback;
            clientSideCommand = clientSide;
            serverSideCommand = serverSide;
            consoleCommand = console;
        }
        /// <summary>
        /// Send help message to caller.
        /// </summary>
        /// <param name="caller"></param>
        public void printHelp(Caller caller) {
            string message = $"/{Command} - {HelpMessage}";
            NotifyCaller(caller, message);
        }

        /// <summary>
        /// Get the help message for this command.
        /// </summary>
        /// <returns></returns>
        public string getHelpMessage() { 
            return HelpMessage;
        }
    }
}
