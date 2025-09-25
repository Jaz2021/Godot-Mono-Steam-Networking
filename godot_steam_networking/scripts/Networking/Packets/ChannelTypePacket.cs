
using System;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Godot;
using Networking_V2;
using Steamworks;
[Packet(0)]
public class ChannelTypePacket : IPacket<ChannelTypePacket>
{
    public ChannelTypePacket(ChannelType type, CSteamID steamId){
        channelType = type;
        id = steamId;

    }
    public enum ChannelType : byte
    {
        Gameplay = 0,
        Audio = 1
    }
    public ChannelType channelType;
    public CSteamID id;
    public static ChannelTypePacket Deserialize(IntPtr data, ref int offset, int size)
    {
        // Skip the first byte 
        GD.Print($"Offset: {offset}");
        byte channelTypeByte = Marshal.ReadByte(data, offset);
        offset++;
        ulong steamID = PtrConverter.GetULong(data, ref offset);
        return new((ChannelType)channelTypeByte, (CSteamID)steamID);
    }
    public static void Signal(ChannelTypePacket packet, ConnectionManager connection)
    {
        // this will be very different specifically for channelType packets
        // We won't have a specific delegate here to call, 
        // we are signalling back to the original connection instead
        GD.Print($"Received channel type packet: {packet.channelType} {packet.id}");
        NetworkingV2.SetConnectionType(connection, packet.channelType, packet.id);
    }

    public byte[] Serialize()
    {
        GD.Print($"Serializing channelTYpe packet {id}");
        // Reminder to add the initial byte as 0 since thats the packet type
        return [0, 0, 0, (byte)channelType, ..BitConverter.GetBytes((ulong)id)];
    }
}