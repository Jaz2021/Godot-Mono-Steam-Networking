public static class SourceGenerationHelper
{
    public const string Attribute = @"
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
}";
    public const string SerializeDataAttribute = @"
using System;
namespace Networking_V2;
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class SerializeDataAttribute : Attribute {
    public SerializeDataAttribute(){
    }
}
    ";
    public const string Net = """
    using System;
    using Steamworks;
    using System.Runtime.InteropServices;
    using Godot;
    namespace Networking_V2{
    public static partial class NetworkingV2 {
    public static void ReceivePacket(ref IntPtr data, ConnectionManager connection){
        var pkt = Marshal.PtrToStructure<SteamNetworkingMessage_t>(data);
        var length = pkt.m_cbSize;
        var packet = pkt.m_pData;
        int offset = 0;
        while(offset + 3 < pkt.m_cbSize){
            var type = Marshal.ReadByte(packet, offset);
            offset++;
            var type2 = Marshal.ReadByte(packet, offset);
            offset++;
            var type3 = Marshal.ReadByte(packet, offset);
            offset++;
            byte properType = 0;
            // bool foundProperType = false;
            if (type == type2 || type == type3)
            {
                properType = type;
            }
            else if (type2 == type3)
            {
                properType = type2;
            } else {
                // GD.Print($"Message types disagreed [{type}, {type2}, {type3}], dropping rest of packets this tick");
                SteamNetworkingMessage_t.Release(data);
                return;
            }
                
            switch (properType)
            {
                /*CASE*/
                default:
                    // GD.Print($"Recieved unset packet type, {properType}");
                    SteamNetworkingMessage_t.Release(data);
                    return;
            }
        }
        SteamNetworkingMessage_t.Release(data);
    }
}
}
""";
    public const string Case = """
    case /*type*/:
        // GD.Print("Received packet of type /*class*/");
        IPacket</*class*/>.DeserializeAndSignal(packet, ref offset, connection, length);
        break;
    """;
    // Serializer inputs:
    // class: The class name
    // id: the number the packet is using
    // serializers: The list of serializers in alphabetical order by name of the variable (to keep things consistent)
    // class_vars: The list of serializable data that gets turned into a new value
    // class_var_inputs: The type name, type name, stuff for the instantiator
    // class_var_setters: The lines of this.var_name = varname
    // deserializers: The list of deserializers, in alphabetical order again. Using PtrConverter.

    public const string SerializerClass = """
    namespace Networking_V2;
    using System;
    using Steamworks;
    using Godot;
    /*using*/
    public partial class /*class*/ : IPacket</*class*/>
    {
        /*signal*/
        /*constructor*/
        
        /*serializer*/
    }
    """;
    public const string SerializerFuncs = """
        public byte[] Serialize(){
            return [
                /*id*/, /*id*/, /*id*/,
                /*serializers*/
            ];
        }
        public static /*class*/ Deserialize(IntPtr data, ref int offset, int size)
        {
            /*deserializers*/
            return new(/*class_vars*/);
        }
    """;
    public const string Constructor = """
    public /*class*/(/*class_var_inputs*/){
            /*class_var_setters*/
        }
    """;
    public const string Signal = """
    public delegate void /*class*/Signal(/*class*/ packet, ConnectionManager connection);
    public static /*class*/Signal /*class*/Received;
    public static void Signal(/*class*/ packet, ConnectionManager connection){
        /*class*/Received?.Invoke(packet, connection);
    }
    """;
    // name: The variable name to be deserialized
    // type: The type to be deserialized
    public const string Deserializer = """
    var /*name*/ = PtrConverter.Get/*type*/(data, ref offset);
    """;
}
