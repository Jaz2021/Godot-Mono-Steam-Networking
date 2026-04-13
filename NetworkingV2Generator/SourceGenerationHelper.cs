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
    public static void DeserializePacket(byte[] data, ConnectionManager connection){
        int offset = 0;
        int length = data.Length;
        // Debugger.Print($"Received a packet {data[0]}, {data[1]}, {data[2]}");
        #if BATCHING_ENABLED
        while(offset + 3 < length){
        #endif
            var type = data[offset];
            offset++;
            var type2 = data[offset];
            offset++;
            var type3 = data[offset];
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
                return;
            }
                
            switch (properType)
            {
                /*CASE*/
                default:
                    // GD.Print($"Recieved unset packet type, {properType}");
                    return;
            }
        #if BATCHING_ENABLED
        }
        #endif
    }
}
}
""";
    public const string Case = """
    case /*type*/:
        GD.Print("Received packet of type /*class*/");
        IPacket</*class*/>.DeserializeAndSignal(data, ref offset, connection, length);
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
        public static /*class*/ Deserialize(byte[] data, ref int offset, int size)
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
        // GD.Print("/*class*/");
        /*class*/Received?.Invoke(packet, connection);
    }
    """;
    // name: The variable name to be deserialized
    // type: The type to be deserialized
    public const string Deserializer = """
    var /*name*/ = PtrConverter.Get/*type*/(data, ref offset);
    """;
}
