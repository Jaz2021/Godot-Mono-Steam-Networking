using Godot;
using Networking_V2;
using System;

public partial class StuffAndThings : Node2D
{
    [Export]
    private bool OpenLobby {
        set{
            if(value){
                NetworkingV2.Init(this, true);
                if(NetworkingV2.isInit){
                    NetworkingV2.CreateLobby();
                    InLobby = true;
                }

            }
        }
        get => false;
    }
    [Export] private float PacketSendTimeout = 0.1f;
    [Export] private string PacketData = "123456";
    private float accumulatedDelta = 0f;
    private bool InLobby = false;
    public override void _Ready()
    {
        NetworkingV2.StartGame += StartGame;
        NetworkingV2.QuitGame += QuitGame;
        VeryLongPacket.VeryLongPacketReceived += PacketRecvd;
        NetworkingV2.Init(this, false);
    }

    private void QuitGame()
    {
        // throw new NotImplementedException();
    }

    private void PacketRecvd(VeryLongPacket packet, ConnectionManager connection)
    {
        // throw new NotImplementedException();
        if(packet.long_data == PacketData){
            Debugger.Print($"Received packet with normal data");
        } else {
            Debugger.Print("Received packet with malformed data");
        }
    }

    public override void _Process(double delta)
    {
        if(InLobby && NetworkingV2.GetLobbyMembers().Count != 0){
            accumulatedDelta += (float)delta;
            if(accumulatedDelta >= PacketSendTimeout){
                VeryLongPacket packet = new(PacketData);
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
