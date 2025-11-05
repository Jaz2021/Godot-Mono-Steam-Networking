using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;
using Steamworks;

public static class SerializationExtension
{
    public static byte[] Serialize(this string value)
    {
        if (value == "")
        {
            return [0];
        }
        value += "\0";
        List<byte> bytes = new();
        foreach (var c in value)
        {
            bytes.Add((byte)c);
        }
        return bytes.ToArray();
    }
    public static byte[] Serialize(this byte value)
    {
        return [value];
    }
    public static byte[] Serialize(this bool value)
    {
        return BitConverter.GetBytes(value);
    }

    public static byte[] Serialize(this char value)
    {
        return [(byte)value];
    }

    public static byte[] Serialize(this short value)
    {
        return BitConverter.GetBytes(value);
    }

    public static byte[] Serialize(this ushort value)
    {
        return BitConverter.GetBytes(value);
    }

    public static byte[] Serialize(this int value)
    {
        return BitConverter.GetBytes(value);
    }

    public static byte[] Serialize(this uint value)
    {
        return BitConverter.GetBytes(value);
    }

    public static byte[] Serialize(this long value)
    {
        return BitConverter.GetBytes(value);
    }

    public static byte[] Serialize(this ulong value)
    {
        return BitConverter.GetBytes(value);
    }

    public static byte[] Serialize(this float value)
    {
        return BitConverter.GetBytes(value);
    }

    public static byte[] Serialize(this double value)
    {
        return BitConverter.GetBytes(value);
    }
    public static byte[] Serialize(this Vector3 value)
    {
        return [.. BitConverter.GetBytes(value.X),
            .. BitConverter.GetBytes(value.Y),
            .. BitConverter.GetBytes(value.Z)];
    }
    public static byte[] Serialize(this CSteamID value)
    {
        return ((ulong)value).Serialize();
    }
    

}