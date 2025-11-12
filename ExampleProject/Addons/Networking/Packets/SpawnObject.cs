using Godot;

namespace Networking_V2;
[Packet]
public partial class SpawnObjectPacket : IPacket<SpawnObjectPacket>
{
    [SerializeData]
    private byte objtype;
    public ObjectType ObjType
    {
        get
        {
            return (ObjectType)objtype;
        }
    }
    [SerializeData]
    public Vector3 position;
    [SerializeData]
    public Vector3 rotation;
    [SerializeData]
    public ulong id;
}