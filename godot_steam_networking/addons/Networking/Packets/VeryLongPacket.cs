namespace Networking_V2;
[Packet]
public partial class VeryLongPacket : IPacket<VeryLongPacket> 
{
    [SerializeData] public string long_data;
}