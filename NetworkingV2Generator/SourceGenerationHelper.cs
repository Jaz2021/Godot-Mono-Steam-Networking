public static class SourceGenerationHelper
{
    public const string Attribute = @"
using System;
namespace Networking_V2;
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PacketAttribute : Attribute {
    public byte Id {get;}
    public PacketAttribute(byte id){
        Id = id;
    }
}";
    public const string Net = """
    using System;
    using Steamworks;
    using System.Runtime.InteropServices;
    using Godot;
    namespace Networking_V2{
    public static partial class NetworkingV2 {
    public static void ReceivePacket(ref IntPtr data, ConnectionManager connection){
        var pkt = Marshal.PtrToStructure<SteamNetworkingMessage_t>(data);
        var length = pkt.m_cbSize;
        var packet = pkt.m_pData;
        int offset = 0;
        while(offset + 3 < pkt.m_cbSize){
            var type = Marshal.ReadByte(packet, offset);
            offset++;
            var type2 = Marshal.ReadByte(packet, offset);
            offset++;
            var type3 = Marshal.ReadByte(packet, offset);
            offset++;
            byte properType = 0;
            bool foundProperType = false;
            if (type == type2 || type == type3)
            {
                properType = type;
            }
            else if (type2 == type3)
            {
                properType = type2;
            } else {
                GD.Print($"Message types disagreed [{type}, {type2}, {type3}], dropping rest of packets this tick");
                SteamNetworkingMessage_t.Release(data);
                return;
            }
                
            switch (properType)
            {
                /*CASE*/
                default:
                    GD.Print($"Recieved unset packet type, {properType}");
                    SteamNetworkingMessage_t.Release(data);
                    return;
            }
        }
        SteamNetworkingMessage_t.Release(data);
    }
}
}
""";
    public const string Case = """
    case /*type*/:
        GD.Print("Received packet of type /*class*/");
        IPacket</*class*/>.DeserializeAndSignal(packet, ref offset, connection, length);
        break;
""";
}
