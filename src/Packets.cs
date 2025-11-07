using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeTalker.Networking;
using CodeTalker.Packets;

namespace AtlyssCommandLib;

internal class ServerCommandPkt : BinaryPacketBase {

    public override string PacketSignature => "ACL_ServerCmds";
    public List<Entry> entries = new List<Entry>();

    internal struct Entry {
        public string prefix;
        public string helpMessage;
    }

    public ServerCommandPkt() { }

    public override byte[] Serialize() {
        CommandManager.updateServerCommands();

        var ms = new MemoryStream();
        var buffer = new BinaryWriter(ms);

        buffer.Write((byte)CommandManager.serverCommands.Count);
        foreach (var cmd in CommandManager.serverCommands) {
            buffer.Write(Encoding.UTF8.GetBytes(cmd.Key + '\0'));
            buffer.Write(Encoding.UTF8.GetBytes(cmd.Value + '\0'));
        }

        Plugin.logger?.LogMessage($"Full servercmd hex: {BitConverter.ToString(ms.ToArray()).Replace("-", "")}");
        return ms.ToArray();
    }

    public override void Deserialize(byte[] data) {

        var ms = new MemoryStream(data);
        var buffer = new BinaryReader(ms);

        byte count = buffer.ReadByte();
        short start = 1, end1 = 0, end2 = 0;
        while (ms.Position < ms.Length) {
            if (buffer.Read() == 0) {
                if (end1 == 0) {
                    end1 = (short)ms.Position;
                } else if (end2 == 0) {
                    end2 = (short)ms.Position;

                    string prefix = Encoding.UTF8.GetString(data, start, end1 - start - 1);
                    string helpMessage = Encoding.UTF8.GetString(data, end1, end2 - end1 - 1);

                    entries.Add(new Entry { prefix = prefix, helpMessage = helpMessage });

                    end1 = 0;
                    end2 = 0;
                    start = (short)ms.Position;
                }
            }
        }

        if(entries.Count != count) {
            Plugin.logger?.LogWarning($"Expected {count} server commands but got {entries.Count}!");
        }
    }
}
