using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;
using Networking_V2;
using Steamworks;

public partial class ConnectionManager : Node {
    private bool ConnectionEstablished = true;
    private HSteamNetConnection connection;
    private IntPtr data = IntPtr.Zero;
    private int dataLength;
    private const int maxDataLength = 1200;
    public CSteamID steamID {
        private set;
        get;
    }

    public ConnectionManager(SteamNetworkingIdentity netId, ChannelTypePacket.ChannelType type)
    {
        GD.Print($"Created outgoing connection manager for: {netId.GetSteamID()}");
        // This is for creating an outgoing connection
        // ConnectionId should always be 0 or 1 as of right now because those are the open sockets
        steamID = netId.GetSteamID();
        connection = SteamNetworkingSockets.ConnectP2P(ref netId, (int)type,  0, null);
        NetworkingV2.AddUnboundSocket(connection);
        ChannelTypePacket packet = new(type, NetworkingV2.steamID);
        SendPacketReliable(packet);
        // Globals.instance.root.AddChild(this); // Add myself as a child so we enter the tree and can process
    }
    public ConnectionManager(HSteamNetConnection connection){
        // For creating an incoming connection.
        // What would come up when we accept a p2p connection is this guy
        // this.steamID = steamID;
        GD.Print($"Created incoming connection manager for connection: {connection.m_HSteamNetConnection}");

        this.connection = connection;
        // Globals.instance.root.AddChild(this);
    }
    public void SendPacketUnreliable<T>(IPacket<T> packet, bool individualPacket = false) where T : IPacket<T>{
        var pdata = packet.Serialize();
        if(pdata.Length > maxDataLength){
            // var data = packet.Serialize();
            var intptr = GCHandle.Alloc(pdata, GCHandleType.Pinned);
            IntPtr ptr = intptr.AddrOfPinnedObject();
            Marshal.Copy(pdata, 0, ptr, pdata.Length);
            var res = SteamNetworkingSockets.SendMessageToConnection(connection, ptr, (uint)pdata.Length, NetworkingV2.SEND_RELIABLE, out _);
            GD.Print($"Sent packet to {connection.m_HSteamNetConnection} with result {res}");
            // GD.Print("Sent a packet, " + res.ToString());
            // GD.Print(connection.m_HSteamNetConnection);
            intptr.Free();
            return;
        }
        if(pdata.Length + dataLength > maxDataLength){
            SendDataOverConnection(); // SendDataOverConnection will clear out the IntPtr
        }
        if(dataLength == 0){
            data = Marshal.AllocHGlobal(maxDataLength);
        }
        Marshal.Copy(pdata, 0, data + dataLength, pdata.Length);
        dataLength += pdata.Length;
    }
    private void SendDataOverConnection(){
        var res = SteamNetworkingSockets.SendMessageToConnection(connection, data, (uint)dataLength, NetworkingV2.SEND_UNRELIABLE, out _);
        GD.Print($"Sent packet to {connection.m_HSteamNetConnection} with result {res}");
        dataLength = 0;
    }
    public void SendPacketReliable<T>(IPacket<T> packet) where T : IPacket<T>{
        // Reliable sends happen instantly, no need to package them up. They don't rely on speed in the first place
        var data = packet.Serialize();
        var intptr = GCHandle.Alloc(data, GCHandleType.Pinned);
        IntPtr ptr = intptr.AddrOfPinnedObject();
        Marshal.Copy(data, 0, ptr, data.Length);
        var res = SteamNetworkingSockets.SendMessageToConnection(connection, ptr, (uint)data.Length, NetworkingV2.SEND_RELIABLE, out _);
        GD.Print($"Sent packet of length {data.Length} to {connection.m_HSteamNetConnection} with result {res}");
        // GD.Print("Sent a packet, " + res.ToString());
        // GD.Print(connection.m_HSteamNetConnection);
        intptr.Free();
    }
    public void DropConnection(){
        SteamNetworkingSockets.CloseConnection(connection, 0, "Disconnected by user", false);
    }
    public override void _Ready()
    {
        // Not sure if we do anything here but oh well
    }
    public void SetSteamId(CSteamID id){
        if(steamID == (CSteamID)0){
            GD.Print($"Set connection: {connection.m_HSteamNetConnection} to steamid: {id}");
            steamID = id;
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        if(dataLength > 0){
            SendDataOverConnection();
        }
    }
    public override void _Process(double delta)
    {
        IntPtr[] packets = new nint[10];
        var numPackets = SteamNetworkingSockets.ReceiveMessagesOnConnection(connection, packets, 10);
        for (int i = 0; i < numPackets; i++){
            // GD.Print("Received a packet");
            NetworkingV2.ReceivePacket(ref packets[i], this);
        }
    }
    public override void _ExitTree()
    {
        // Clean up data
        if(data != IntPtr.Zero){
            Marshal.FreeHGlobal(data);
        }
    }
}