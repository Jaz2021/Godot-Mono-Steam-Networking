using System.Collections.Generic;
using Godot;
using Networking_V2;

public partial class Scene : Node
{
    [Export] private Node3D objectRoot; // ObjectRoot should move according to the lobby owner's position. 
    [Export] private MainPlayer playerController;
    [Export] private Godot.Collections.Dictionary<ObjectType, PackedScene> objectPrefabs;
    public List<NetObject> objects = new();
    public override void _Ready()
    {
        SpawnObjectPacket.SpawnObjectPacketReceived += SpawnObjectPacketReceived;
    }
    private void SpawnObjectPacketReceived(SpawnObjectPacket packet, ConnectionManager connection)
    {
        if(connection != null && connection.steamID != NetworkingV2.GetLobbyOwner())
        {
            return;
        }
        if (packet.ObjType == ObjectType.Player && packet.id == (ulong)NetworkingV2.steamID)
        {
            GD.Print("Just got a spawn object packet for my own player object, ignored");
            return;
        }
        NetObject obj = objectPrefabs[packet.ObjType].Instantiate<NetObject>();
        obj.Init(packet.id, packet.ObjType, objectRoot, packet.position, packet.rotation);
        objectRoot.CallDeferred("add_child", obj);
        objects.Add(obj);

    }
    public void SpawnObject(ObjectType type, ulong id, Vector3 position, Vector3 rotation, bool send = false)
    {
        NetObject netobj = objectPrefabs[type].Instantiate<NetObject>();
        netobj.Init(id, type, objectRoot, position, rotation);
        objectRoot.CallDeferred("add_child", netobj);
        objects.Add(netobj);
        if (send)
        {
            SpawnObjectPacket packet = new(id, (byte)type, position, rotation);
            NetworkingV2.SendPacketToAll(packet, true);
        }
    }
    public void EnterScene(ObjectStructList objectSpawns, Vector3 position, Vector3 rotation)
    {
        PlayerObject myPlayer = objectPrefabs[ObjectType.Player].Instantiate<PlayerObject>();
        if (NetworkingV2.isInit)
        {
            myPlayer.Init((ulong)NetworkingV2.steamID, ObjectType.Player, objectRoot, position, rotation);
        }
        else
        {
            myPlayer.Init(0, ObjectType.Player, objectRoot, position, rotation);
        }
        objectRoot.CallDeferred("add_child", myPlayer);
        playerController.SetPlayerObject(myPlayer);
        objects.Add(myPlayer);
        if (objectSpawns == null)
        {
            return;
        }
        foreach(var obj in objectSpawns.objs)
        {
            SpawnObject(obj.objType, obj.id, obj.position, obj.rotation);
        }
    }
}