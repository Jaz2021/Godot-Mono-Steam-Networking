using System;
namespace Networking_V2;
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PacketAttribute : Attribute {
    [Flags]
    public enum PacketGenerationFlags
    {
        GenerateNothing = 0,
        GenerateSerializerDeserializer = 1,
        GenerateConstructor = 2,
        GenerateSignal = 4
    }
    PacketGenerationFlags flags;
    public PacketAttribute(PacketGenerationFlags flags)
    {
        this.flags = flags;
    }
    public PacketAttribute()
    {
        flags = PacketGenerationFlags.GenerateConstructor | PacketGenerationFlags.GenerateSerializerDeserializer | PacketGenerationFlags.GenerateSignal;
    }
}