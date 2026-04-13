using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using Networking_V2;
using Steamworks;


public static class PtrConverter
{
    public static short GetShort(byte[] packet, ref int start)
    {
        if (start + sizeof(short) > packet.Length)
        {
            return 0;
        }
        short value = BitConverter.ToInt16(packet, start);
        start += sizeof(short);
        return value;
    }
    public static short GetInt16(byte[] packet, ref int start)
    {
        return GetShort(packet, ref start);
    }


    public static ushort GetUShort(byte[] packet, ref int start)
    {
        if (start + sizeof(ushort) > packet.Length)
        {
            return 0;
        }
        ushort value = BitConverter.ToUInt16(packet, start);
        start += sizeof(ushort);
        return value;
    }
    public static ushort GetUInt16(byte[] packet, ref int start)
    {
        return GetUShort(packet, ref start);
    }

    public static int GetInt32(byte[] packet, ref int start)
    {
        if (start + sizeof(int) > packet.Length)
        {
            return 0;
        }
        int value = BitConverter.ToInt32(packet, start);
        start += sizeof(Int32);
        return value;
    }
    public static uint GetUInt32(byte[] packet, ref int start)
    {
        if (start + sizeof(uint) > packet.Length)
        {
            return 0;
        }
        uint value = BitConverter.ToUInt32(packet, start);
        start += sizeof(uint);
        return value;
    }

    public static long GetLong(byte[] packet, ref int start)
    {
        if (start + sizeof(long) > packet.Length)
        {
            return 0;
        }
        long value = BitConverter.ToInt64(packet, start);
        start += sizeof(long);
        return value;
    }
    public static Int64 GetInt64(byte[] packet, ref int start)
    {
        return GetLong(packet, ref start);
    }

    public static ulong GetULong(byte[] packet, ref int start)
    {
        if (start + sizeof(ulong) > packet.Length)
        {
            return 0;
        }
        ulong value = BitConverter.ToUInt64(packet, start);
        start += sizeof(ulong);
        return value;
    }
    public static ulong GetUInt64(byte[] packet, ref int start)
    {
        return GetULong(packet, ref start);
    }

    public static float GetFloat(byte[] packet, ref int start)
    {
        if (start + sizeof(float) > packet.Length)
        {
            return 0f;
        }
        float value = BitConverter.ToSingle(packet, start);
        start += sizeof(float);

        return value;
    }

    public static double GetDouble(byte[] packet, ref int start)
    {
        if (start + sizeof(double) > packet.Length)
        {
            return 0;
        }
        double value = BitConverter.ToDouble(packet, start);
        start += sizeof(double);
        return value;
    }
    public static float GetSingle(byte[] packet, ref int start)
    {

        return GetFloat(packet, ref start);
    }

    public static bool GetBoolean(byte[] packet, ref int start)
    {
        if (start + sizeof(bool) > packet.Length)
        {
            return false;
        }
        bool value = BitConverter.ToBoolean(packet, start);
        start += sizeof(bool);
        return value;
    }

    public static char GetChar(byte[] packet, ref int start)
    {
        if (start + sizeof(short) > packet.Length)
        {
            return '\0';
        }
        // Console.Log($"{sizeof(char)}");
        char value = (char)ReadBytes(packet, ref start, 1)[0];
        return value;
    }
    public static Vector3 GetVector3(byte[] packet, ref int start)
    {
        var x = GetFloat(packet, ref start);
        var y = GetFloat(packet, ref start);
        var z = GetFloat(packet, ref start);
        return new(x, y, z);

    }
    public static CSteamID GetCSteamID(byte[] packet, ref int start)
    {
        return (CSteamID)GetULong(packet, ref start);
    }
    public static byte GetByte(byte[] packet, ref int start)
    {
        if (start + 1 > packet.Length)
        {
            return 0;
        }
        byte value = ReadBytes(packet, ref start, 1)[0];
        return value;
    }

    // Infinite length string terminated by \0
    // public static string GetString(byte[] packet, ref int start)
    // {
    //     // This could theoretically crash with an access out of bounds error, if you want to make it safer
    //     // implement a check or a max length or something other than ending on a 0 byte
    //     StringBuilder sb = new();
    //     while(true) {
    //         char c = GetChar(packet, ref start);
    //         if (c == '\0')
    //         {
    //             return sb.ToString();
    //         }
    //         sb.Append(c);
    //     }
    // }
    // Safer max 2^16 length string
    public static string GetString(byte[] packet, ref int start)
    {
        ushort strLength = GetUShort(packet, ref start);
        if (start + strLength > packet.Length)
        {
            return "";
        }
        StringBuilder sb = new();
        for (int i = 0; i < strLength; i++)
        {
            sb.Append(GetChar(packet, ref start));
        }
        return sb.ToString();
    }
    public static Basis GetBasis(byte[] packet, ref int start){
        Vector3 col1 = GetVector3(packet, ref start);
        Vector3 col2 = GetVector3(packet, ref start);
        Vector3 col3 = GetVector3(packet, ref start);
        return new(col1, col2, col3);
    }
    public static Transform3D GetTransform3D(byte[] packet, ref int start){
        Vector3 col1 = GetVector3(packet, ref start);
        Vector3 col2 = GetVector3(packet, ref start);
        Vector3 col3 = GetVector3(packet, ref start);
        Vector3 origin = GetVector3(packet, ref start);
        return new(col1, col2, col3, origin);
    }
    public static T GetVariant<T>(byte[] packet, ref int start) where T : class
    {
        int size = Marshal.SizeOf<T>();
        byte[] data = ReadBytes(packet, ref start, size);
        var outData = GD.BytesToVar(data);
        return outData as T;
    }

    private static byte[] ReadBytes(byte[] packet, ref int start, int size)
    {
        if (start + size > packet.Length)
        {
            return [];
        }
        byte[] data = new byte[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = packet[start + i];
        }
        start += size;
        return data;
    }
    public static Vector2 GetVector2(byte[] packet, ref int start){
        float x = GetFloat(packet, ref start);
        float y = GetFloat(packet, ref start);
        return new(x, y);
    }
    // public static byte[] Vector3ToBytes(Vector3 vec)
    // {
    //     var xBytes = BitConverter.GetBytes(vec.X);
    //     var yBytes = BitConverter.GetBytes(vec.Y);
    //     var zBytes = BitConverter.GetBytes(vec.Z);
    //     return [.. xBytes, .. yBytes, .. zBytes]; // Collection expression and spread operator to do this. Interesting
    // }
}