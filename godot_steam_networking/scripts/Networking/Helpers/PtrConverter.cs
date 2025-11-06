using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Godot;
using Steamworks;


public static class PtrConverter
{
    public static short GetShort(IntPtr packet, ref int start)
    {
        short value = BitConverter.ToInt16(ReadBytes(packet, ref start, sizeof(short)), 0);
        return value;
    }
    public static short GetInt16(IntPtr packet, ref int start)
    {
        return GetShort(packet, ref start);
    }


    public static ushort GetUShort(IntPtr packet, ref int start)
    {
        ushort value = BitConverter.ToUInt16(ReadBytes(packet, ref start, sizeof(ushort)), 0);
        return value;
    }
    public static ushort GetUInt16(IntPtr packet, ref int start)
    {
        return GetUShort(packet, ref start);
    }

    public static int GetInt32(IntPtr packet, ref int start)
    {
        int value = BitConverter.ToInt32(ReadBytes(packet, ref start, sizeof(int)), 0);
        return value;
    }
    public static uint GetUInt32(IntPtr packet, ref int start)
    {
        uint value = BitConverter.ToUInt32(ReadBytes(packet, ref start, sizeof(uint)), 0);
        return value;
    }

    public static long GetLong(IntPtr packet, ref int start)
    {
        long value = BitConverter.ToInt64(ReadBytes(packet, ref start, sizeof(long)), 0);
        return value;
    }
    public static Int64 GetInt64(IntPtr packet, ref int start)
    {
        return GetLong(packet, ref start);
    }

    public static ulong GetULong(IntPtr packet, ref int start)
    {
        ulong value = BitConverter.ToUInt64(ReadBytes(packet, ref start, sizeof(ulong)), 0);
        return value;
    }
    public static ulong GetUInt64(IntPtr packet, ref int start)
    {
        return GetULong(packet, ref start);
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
    public static float GetSingle(IntPtr packet, ref int start)
    {
        return GetFloat(packet, ref start);
    }

    public static bool GetBoolean(IntPtr packet, ref int start)
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
    public static Vector3 GetVector3(IntPtr packet, ref int start)
    {
        var x = GetFloat(packet, ref start);
        var y = GetFloat(packet, ref start);
        var z = GetFloat(packet, ref start);
        return new(x, y, z);

    }
    public static CSteamID GetCSteamID(IntPtr packet, ref int start)
    {
        return (CSteamID)GetULong(packet, ref start);
    }
    public static byte GetByte(IntPtr packet, ref int start)
    {
        byte value = ReadBytes(packet, ref start, 1)[0];
        return value;
    }
    // Infinite length string terminated by \0
    // public static string GetString(IntPtr packet, ref int start)
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
    public static string GetString(IntPtr packet, ref int start)
    {
        ushort strLength = GetUShort(packet, ref start);
        StringBuilder sb = new();
        for (int i = 0; i < strLength; i++)
        {
            sb.Append(GetChar(packet, ref start));
        }
        return sb.ToString();
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