using Godot;

namespace Networking_V2;
[Packet]
public partial class StartGamePacket : IPacket<StartGamePacket>
{
    [SerializeData]
    public Vector3 StartPosition;
    [SerializeData]
    public Vector3 StartRotation;
    [SerializeData]
    public ObjectStructList ExistingObjects;
}