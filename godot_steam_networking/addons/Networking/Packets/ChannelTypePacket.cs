
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
    [SerializeData]
    public byte cType;
    [SerializeData]
    public CSteamID id;
}