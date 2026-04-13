using System;
using System.Text;
using Godot;
using Steamworks;
namespace Networking_V2
{
    public interface IPacket<T> where T : IPacket<T>{
        public static abstract void Signal(T packet, ConnectionManager connection);
        public static abstract T Deserialize(byte[] data, ref int offset, int totalLength);
        public static void DeserializeAndSignal(byte[] data, ref int offset, ConnectionManager connection, int totalLength){
            var packet = T.Deserialize(data, ref offset, totalLength);
            T.Signal(packet, connection);

        }
        public abstract byte[] Serialize();

    }
}