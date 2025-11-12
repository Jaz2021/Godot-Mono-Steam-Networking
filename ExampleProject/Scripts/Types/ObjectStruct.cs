using System.Collections.Generic;
using Godot;

public struct ObjectStruct
{
    public ObjectStruct(ulong id, Vector3 position, Vector3 rotation, ObjectType objectType)
    {
        this.id = id;
        this.position = position;
        this.rotation = rotation;
        objType = objectType;
    }
    public readonly ulong id;
    public readonly Vector3 position;
    public readonly Vector3 rotation;
    public readonly ObjectType objType;
    public byte[] Serialize()
    {
        return [
            ..id.Serialize(),
            ..position.Serialize(),
            ..rotation.Serialize(),
            (byte)objType
        ];
    }
}
public class ObjectStructList
{
    public readonly ObjectStruct[] objs;
    public ObjectStructList(ObjectStruct[] objs)
    {
        this.objs = objs;
    }
    public byte[] Serialize()
    {
        List<byte> data = new();
        data.AddRange(objs.Length.Serialize());
        foreach (var obj in objs)
        {
            data.AddRange(obj.Serialize());
        }
        return data.ToArray();
    }
}
public enum ObjectType : byte
{
    Player
}