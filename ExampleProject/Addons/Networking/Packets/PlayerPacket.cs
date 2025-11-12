namespace Networking_V2;
using Godot;
[Packet]
public partial class PlayerPacket : IPacket<PlayerPacket>
{
    [SerializeData]
    public Vector3 position;
    [SerializeData]
    public Vector3 rotation;
    [SerializeData]
    public Vector3 velocity;
    [SerializeData]
    public Vector3 angularVelocity;
    [SerializeData]
    public ulong id;
}