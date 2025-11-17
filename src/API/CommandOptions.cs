using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;

namespace AtlyssCommandLib.src.API {

    /// <summary>
    /// Options for a command. This is a struct to avoid reference issues so you can reuse this without worrying.
    /// </summary>
    public struct CommandOptions {

        /// <summary>
        /// This command runs on the client.
        /// </summary>
        public bool clientSide;

        /// <summary>
        /// This command to runs on the server and will be announced to clients. Return true if client to forward to the server.
        /// </summary>
        public bool serverSide;

        /// <summary>
        /// Allow this command to run from the console. **Always check if the caller has permissions if this can be run from client or server!**
        /// </summary>
        public bool consoleCmd;

        /// <summary>
        /// This command can only be run by the host and will be hidden.
        /// </summary>
        public bool hostOnly;

        /// <summary>
        /// Default options: clientSide = true, serverSide = false, consoleCmd = false
        /// </summary>
        public CommandOptions() {
            clientSide = true;
            serverSide = false;
            consoleCmd = false;
            hostOnly = false;
        }

        /// <summary>
        /// Full optional constructor, everything defaults to false
        /// </summary>
        /// <param name="clientSide"></param>
        /// <param name="serverSide"></param>
        /// <param name="consoleCmd"></param>
        /// <param name="hostOnly"></param>
        public CommandOptions(bool clientSide = false, bool serverSide = false, bool consoleCmd = false, bool hostOnly = false) {
            this.clientSide = clientSide;
            this.serverSide = serverSide;
            this.consoleCmd = consoleCmd;
            this.hostOnly = hostOnly;
        }
    }
}
