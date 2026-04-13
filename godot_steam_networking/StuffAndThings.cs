using Godot;
using Networking_V2;
using System;

public partial class StuffAndThings : Node2D
{
    [ExportToolButton("Open lobby")]
    private bool OpenLobby {
        set{
            NetworkingV2.CreateLobby();
            InLobby = true;
        }
        get => false;
    }
    [Export] private float PacketSendTimeout = 0.1f;
    private float accumulatedDelta = 0f;
    private bool InLobby = false;
    public override void _Ready()
    {
        NetworkingV2.StartGame += StartGame;
        VeryLongPacket.VeryLongPacketReceived += PacketRecvd;
        NetworkingV2.Init(this, true);
    }

    private void PacketRecvd(VeryLongPacket packet, ConnectionManager connection)
    {
        // throw new NotImplementedException();
    }

    public override void _Process(double delta)
    {
        if(InLobby && NetworkingV2.GetLobbyMembers().Count != 0){
            accumulatedDelta += (float)delta;
            if(accumulatedDelta >= PacketSendTimeout){
                VeryLongPacket packet = new("25674246792104874934822980141931959864283908644156242972735646751567820875438417406459986988104926605712289119417336554371479183195909950166498736937116518902860215153729893576185791261097713386010067462393475587777949505580133827817678994899344051790636214635988655652689742671483793990554208789881027384954026719978795");
                NetworkingV2.SendPacketToAll(packet);
            }
        }
    }

    private void StartGame(ConnectionManager connection)
    {
        StartGamePacket packet = new();
        connection.SendPacketReliable(packet);
    }
}
