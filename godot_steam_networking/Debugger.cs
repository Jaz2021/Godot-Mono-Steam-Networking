using System;
using Godot;

public static class Debugger
{
    public static string GetTimestamp()
    {

        return DateTime.Now.ToString("HH:mm:ss:fff");
    }
    public static void Print(string msg){
        GD.Print($"[{GetTimestamp()}] - {msg}");
    }
    public static void PrintErr(string msg){
        GD.PrintErr($"[{GetTimestamp()}] - {msg}");
    }
}