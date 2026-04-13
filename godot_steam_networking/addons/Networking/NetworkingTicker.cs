#if !THREADED_NET
using System;
using Godot;
using Networking_V2;

public partial class NetworkingTicker : Node
{
    private float timePertick = 0f;
    private float accumulatedTime = 0f;
    private Action ticker;
    public NetworkingTicker(Action ticker){
        #if NET_DEBUG
        Debugger.Print("Created networking ticker");
        #endif
        timePertick = 1f / (float)NetworkingV2.TICKS_PER_SECOND;
        this.ticker = ticker;
    }
    public override void _Process(double delta)
    {
        accumulatedTime += (float)delta;
        if(accumulatedTime >= timePertick){
            accumulatedTime -= timePertick;
            ticker();
        }
    }
}
#endif