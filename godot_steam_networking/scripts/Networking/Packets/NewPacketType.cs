using System.Numerics;
using Networking_V2;
[Packet(1)]
public partial class NewPacketType : IPacket<NewPacketType>
{
    public static NewPacketType Deserialize(nint data, ref int offset, int totalLength)
    {
        throw new System.NotImplementedException();
    }

    public static void Signal(NewPacketType packet, ConnectionManager connection)
    {
        throw new System.NotImplementedException();
    }

    public byte[] Serialize()
    {
        throw new System.NotImplementedException();
    }
}