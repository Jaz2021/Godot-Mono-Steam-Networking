using Godot;
using Steamworks;
namespace Networking_V2;
[Packet]
public partial class NewPacketType : IPacket<NewPacketType>
{
    [SerializeData]
    public int test;
    [SerializeData]
    public short s;
    [SerializeData]
    public ushort us;
    [SerializeData]
    public uint ui;
    [SerializeData]
    public long l;
    [SerializeData]
    public ulong ul;
    [SerializeData]
    public float f;
    [SerializeData]
    public double d;
    [SerializeData]
    public bool b;
    [SerializeData]
    public string str;
    [SerializeData]
    public char c;
    [SerializeData]
    public Vector3 vec3;
    [SerializeData]
    public CSteamID csid;
    
}