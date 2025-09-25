using System.ComponentModel;
using System;
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PacketAttribute : Attribute {
    public byte Id {get;}
    public PacketAttribute(byte id){
        Id = id;
    }
}