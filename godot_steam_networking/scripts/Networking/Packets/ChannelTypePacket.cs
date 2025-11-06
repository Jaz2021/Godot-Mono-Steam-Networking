
using System;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Godot;
using Networking_V2;
using Steamworks;
namespace Networking_V2;
[Packet]
public partial class ChannelTypePacket : IPacket<ChannelTypePacket>
{
    public enum ChannelType : byte
    {
        Gameplay = 0,
        Audio = 1
    }
    public ChannelType channelType
    {
        get => (ChannelType)cType;
        set
        {
            cType = (byte)value;
        }
    }
    [SerializeData]
    public byte cType;
    [SerializeData]
    public CSteamID id;
}