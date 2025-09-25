using System;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;


public static class PtrConverter
{
    public static short GetShort(IntPtr packet, ref int start)
    {
        short value = BitConverter.ToInt16(ReadBytes(packet, ref start, sizeof(short)), 0);
        return value;
    }

    public static ushort GetUShort(IntPtr packet, ref int start)
    {
        ushort value = BitConverter.ToUInt16(ReadBytes(packet, ref start, sizeof(ushort)), 0);
        return value;
    }

    public static int GetInt(IntPtr packet, ref int start)
    {
        int value = BitConverter.ToInt32(ReadBytes(packet, ref start, sizeof(int)), 0);
        return value;
    }

    public static uint GetUInt(IntPtr packet, ref int start)
    {
        uint value = BitConverter.ToUInt32(ReadBytes(packet, ref start, sizeof(uint)), 0);
        return value;
    }

    public static long GetLong(IntPtr packet, ref int start)
    {
        long value = BitConverter.ToInt64(ReadBytes(packet, ref start, sizeof(long)), 0);
        return value;
    }

    public static ulong GetULong(IntPtr packet, ref int start)
    {
        ulong value = BitConverter.ToUInt64(ReadBytes(packet, ref start, sizeof(ulong)), 0);
        return value;
    }

    public static float GetFloat(IntPtr packet, ref int start)
    {
        float value = BitConverter.ToSingle(ReadBytes(packet, ref start, sizeof(float)), 0);
        return value;
    }

    public static double GetDouble(IntPtr packet, ref int start)
    {
        double value = BitConverter.ToDouble(ReadBytes(packet, ref start, sizeof(double)), 0);
        return value;
    }

    public static bool GetBool(IntPtr packet, ref int start)
    {
        bool value = BitConverter.ToBoolean(ReadBytes(packet, ref start, sizeof(bool)), 0);
        return value;
    }

    public static char GetChar(IntPtr packet, ref int start)
    {
        // Console.Log($"{sizeof(char)}");
        char value = (char)ReadBytes(packet, ref start, 1)[0];
        return value;
    }
    public static Vector3 GetVector3(IntPtr packet, ref int start){
        var x = GetFloat(packet, ref start);
        var y = GetFloat(packet, ref start);
        var z = GetFloat(packet, ref start);
        return new(x, y, z);

    }

    public static T GetVariant<T>(IntPtr packet, ref int start) where T : class
    {
        int size = Marshal.SizeOf<T>();
        byte[] data = ReadBytes(packet, ref start, size);
        var outData = GD.BytesToVar(data);
        return outData as T;
    }

    private static byte[] ReadBytes(IntPtr packet, ref int start, int size)
    {
        byte[] data = new byte[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = Marshal.ReadByte(packet, start + i);
        }
        start += size;
        return data;
    }
    public static byte[] Vector3ToBytes(Vector3 vec){
        var xBytes = BitConverter.GetBytes(vec.X);
        var yBytes = BitConverter.GetBytes(vec.Y);
        var zBytes = BitConverter.GetBytes(vec.Z);
        return [..xBytes, ..yBytes, ..zBytes]; // Collection expression and spread operator to do this. Interesting
    }
}