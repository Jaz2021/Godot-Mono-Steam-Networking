using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;
using Networking_V2;
using Steamworks;

public partial class ConnectionManager {
    // private bool ConnectionEstablished = true;
    private HSteamNetConnection connection;
    #if BATCHING_ENABLED
    private IntPtr data = IntPtr.Zero;
    // private const float TimeBetweenPackets = 1f / 60f; // How many seconds between each packet send
    private int dataLength;
    #endif
    private const int maxDataLength = 1200;

    // private float accumulatedTime = 0f;
    private readonly object locker = new();
    public CSteamID steamID {
        private set;
        get;
    }

    public ConnectionManager(SteamNetworkingIdentity netId, byte type)
    {
        Debugger.Print($"Created outgoing connection manager for: {netId.GetSteamID()}");
        // This is for creating an outgoing connection
        // ConnectionId should always be 0 or 1 as of right now because those are the open sockets
        steamID = netId.GetSteamID();
        #if ICE_DISABLED
        SteamNetworkingConfigValue_t v = new();
        v.m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
        v.m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable;
        SteamNetworkingConfigValue_t.OptionValue n = new();
        n.m_int32 = 0;
        v.m_val = n;
        connection = SteamNetworkingSockets.ConnectP2P(ref netId, (int)type, 1, [v]);
        #endif
        connection = SteamNetworkingSockets.ConnectP2P(ref netId, (int)type, 0, null);

        // int outValue = 0;
        // IntPtr result = Marshal.AllocHGlobal(sizeof(int));
        // ulong cbresult = sizeof(int);
        // ESteamNetworkingConfigDataType d;
        // // int outSize = sizeof(long);
        // var readResult = SteamNetworkingUtils.GetConfigValue(
        //     ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable,
        //     ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Connection,
        //     new IntPtr((long)connection.m_HSteamNetConnection),
        //     out d,
        //     result,
        //     ref cbresult
        // );
        // int outValue = Marshal.ReadInt32(result);
        // Marshal.FreeHGlobal(result);
        // Debugger.Print($"ICE config read result: {readResult}, value: {outValue}");
        NetworkingV2.AddUnboundSocket(connection);
        ChannelTypePacket packet = new((byte)type, NetworkingV2.steamID);
        SendPacketReliable(packet);
        NetworkingV2.OnTick += tick;
        #if BATCHING_ENABLED
        data = Marshal.AllocHGlobal(maxDataLength);
        #endif
    }
    public ConnectionManager(HSteamNetConnection connection){
        // For creating an incoming connection.
        // What would come up when we accept a p2p connection is this guy
        // this.steamID = steamID;
        Debugger.Print($"Created incoming connection manager for connection: {connection.m_HSteamNetConnection}");
        #if BATCHING_ENABLED
        data = Marshal.AllocHGlobal(maxDataLength);
        #endif
        this.connection = connection;
        NetworkingV2.OnTick += tick;
    }
    public void SendPacketUnreliable<T>(IPacket<T> packet, bool individualPacket = false) where T : IPacket<T>{
#if BATCHING_ENABLED
        var pdata = packet.Serialize();
        if(pdata.Length > maxDataLength || individualPacket){
            Debugger.Print("Sent a big packet");
            // var data = packet.Serialize();
            var intptr = GCHandle.Alloc(pdata, GCHandleType.Pinned);
            IntPtr ptr = intptr.AddrOfPinnedObject();
            Marshal.Copy(pdata, 0, ptr, pdata.Length);
            var res = SteamNetworkingSockets.SendMessageToConnection(connection, ptr, (uint)pdata.Length, NetworkingV2.SEND_UNRELIABLE, out _);
            // Debugger.Print($"Sent packet to {connection.m_HSteamNetConnection} with result {res}");
            // Debugger.Print("Sent a packet, " + res.ToString());
            // Debugger.Print(connection.m_HSteamNetConnection);
            intptr.Free();
            return;
        }
        lock(locker){
            if(pdata.Length + dataLength > maxDataLength){
                Debugger.Print("Filled the pdata");
                SendDataOverConnection(); // SendDataOverConnection will clear out the IntPtr
            }
        
            // if(dataLength == 0){
            //     Marshal.FreeHGlobal(data);
            //     data = Marshal.AllocHGlobal(maxDataLength);
            // }
            Marshal.Copy(pdata, 0, data + dataLength, pdata.Length);
            dataLength += pdata.Length;
        }
#else
        var data = packet.Serialize();
        var intptr = GCHandle.Alloc(data, GCHandleType.Pinned);
        IntPtr ptr = intptr.AddrOfPinnedObject();
        Marshal.Copy(data, 0, ptr, data.Length);
        var res = SteamNetworkingSockets.SendMessageToConnection(connection, ptr, (uint)data.Length, NetworkingV2.SEND_UNRELIABLE, out _);
        intptr.Free();
#endif
    }
    #if BATCHING_ENABLED
    private void SendDataOverConnection(){
    
        if(dataLength > 0){
            var res = SteamNetworkingSockets.SendMessageToConnection(connection, data, (uint)dataLength, NetworkingV2.SEND_UNRELIABLE, out _);
            if(res != EResult.k_EResultOK){
                Debugger.Print($"Sent packet to {connection.m_HSteamNetConnection} with result {res}");
            }
            dataLength = 0;
        }
        
    }
    #endif
    public void SendPacketReliable<T>(IPacket<T> packet) where T : IPacket<T>{
        // Reliable sends happen instantly, no need to package them up. They don't rely on speed in the first place
        var data = packet.Serialize();
        var intptr = GCHandle.Alloc(data, GCHandleType.Pinned);
        IntPtr ptr = intptr.AddrOfPinnedObject();
        Marshal.Copy(data, 0, ptr, data.Length);
        var res = SteamNetworkingSockets.SendMessageToConnection(connection, ptr, (uint)data.Length, NetworkingV2.SEND_RELIABLE, out _);
        intptr.Free();
    }
    public void DropConnection(){
        SteamNetworkingSockets.CloseConnection(connection, 0, "Disconnected by user", false);
        NetworkingV2.OnTick -= tick;
    }
    public void SetSteamId(CSteamID id){
        if(steamID == (CSteamID)0){
            Debugger.Print($"Set connection: {connection.m_HSteamNetConnection} to steamid: {id}");
            steamID = id;
        }
    }
    public void tick()
    {

        IntPtr[] packets = new nint[10];
        int packets_received = 0;
        int numPackets;
        do
        {
            numPackets = SteamNetworkingSockets.ReceiveMessagesOnConnection(connection, packets, 10);
            if(numPackets < 0){
                Debugger.PrintErr($"Connection got negative packets {numPackets} error, connection dropped");
                DropConnection();
            }
            for (int i = 0; i < numPackets; i++){
                var pkt = SteamNetworkingMessage_t.FromIntPtr(packets[i]);
                var length = pkt.m_cbSize;
                if (length <= 0 || length > maxDataLength){
                    Debugger.PrintErr($"Received length out of bounds of {length}");
                    continue;
                }
                var packet = pkt.m_pData;
                byte[] managedData = new byte[length];
                Marshal.Copy(packet, managedData, 0, length);
                NetworkingV2.ReceivePacket(managedData, this);
                SteamNetworkingMessage_t.Release(packets[i]);
            }
            packets_received += numPackets;
        } while (numPackets == 10);
        // Debugger.Print($"Received {packets_received} packets this tick");
        SteamNetConnectionRealTimeStatus_t status = new();
        SteamNetConnectionRealTimeLaneStatus_t laneStatus = new SteamNetConnectionRealTimeLaneStatus_t();
        var t = SteamNetworkingSockets.GetConnectionRealTimeStatus(connection, ref status, 1, ref laneStatus);
        if (status.m_cbPendingUnreliable >= 50){
            // SteamNetworkingSockets.FlushMessagesOnConnection(connection);
            #if BATCHING_ENABLED
            dataLength = 0; // When connection is lost stop sending packets until we stop having issues.
            #endif
        } else {
            #if BATCHING_ENABLED
            SendDataOverConnection();
            #else
            SteamNetworkingSockets.FlushMessagesOnConnection(connection);
            #endif
            
        }
    }
    ~ConnectionManager()
    {
        // Clean up data
        #if BATCHING_ENABLED
        if(data != IntPtr.Zero){
            Marshal.FreeHGlobal(data);
        }
        #endif
        NetworkingV2.OnTick -= tick;

    }
}