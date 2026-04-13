// #define NET_DEBUG_FULL // Define this to enable low level networking debugging
// #define NET_DEBUG
// #define THREADED_NET // Define this to run a seperate thread at a set ticks per second
// #define ICE_DISABLED

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
#if THREADED_NET
using System.Threading;
#endif
using Godot;
using Steamworks;
namespace Networking_V2{

    public static partial class NetworkingV2 {
        public const byte NUM_SOCKETS = 1;
        public const int TICKS_PER_SECOND = 60;
        public const int SEND_RELIABLE = 8;
        public const int SEND_UNRELIABLE = 0;
        public const int SEND_UNRELIABLE_NO_DELAY = 1;
        private static SteamLobby lobby = null;
        private const int AppId = 480;
        public static int fps;
        public static bool isInit
        {
            private set;
            get;
        } = false;
        public static Callback<SocketStatusCallback_t> SocketStatusCallback { get; private set; }

        // public static bool isSteamInit
        // {
        //     private set;
        //     get;
        // } = false;
        private static bool online = false;
        public static CSteamID steamID { private set; get; }
        private static string steamName = "";
        private static HSteamListenSocket[] sockets = new HSteamListenSocket[NUM_SOCKETS];
        private static Callback<SteamNetConnectionStatusChangedCallback_t> connectionStatusChangedCallback;
        private static Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;
        private static Callback<LobbyEnter_t> lobbyEnterCallback;
        private static Callback<GameLobbyJoinRequested_t> joinReqCallback;
        private static Callback<LobbyCreated_t> lobbyCreatedCallback;
        private static Callback<P2PSessionRequest_t> p2pSessionCallback;
        private static Callback<PersonaStateChange_t> personaStateChangeCallback;
        private static Callback<SteamRelayNetworkStatus_t> relayNetworkStatusCallback;
        public delegate void PlayerDelegate(CSteamID player);
        public delegate void PlayerReadyDelegate(ConnectionManager gameplayCnxn, ConnectionManager audioCnxn);
        public static PlayerDelegate playerJoinedSignal;
        public static PlayerDelegate playerLeftSignal;
        public static PlayerReadyDelegate playerReadySignal;
        private static bool started = false;
        public delegate void ConnectionTick();
        public static ConnectionTick OnTick;
        public delegate void SendStartGamePacket(ConnectionManager connection);
        public static SendStartGamePacket StartGame;
        public static Action QuitGame;
        #if THREADED_NET
            private static CancellationTokenSource connectionCallerRunning = new();
            private static Thread ticker;
        #else
            private static NetworkingTicker ticker;
        #endif
        // private static List<ConnectionManager> connectionManagers = new();
        private struct Packet {
            public byte[] data;
            public ConnectionManager connection;
        }
        private static ConcurrentQueue<Packet> packetQueue = new();
        public static void Init(Node root, bool force = false){
            if(isInit){
                #if NET_DEBUG
                    Debugger.Print("Networking already initialized, don't need to reinit");
                #endif
                return;
            }
            var init = SteamAPI.Init();
            if(!init){
                #if NET_DEBUG
                    Debugger.Print("Failed to initialize steam");
                #endif
                if (force)
                {
                    Debugger.Print("Shutting down...");
                    // --------------------------------
                    // Quit the game here
                    // --------------------------------
                    QuitGame();

                }
                return;
            }
            #if NET_DEBUG_FULL
            SteamNetworkingUtils.SetDebugOutputFunction(ESteamNetworkingSocketsDebugOutputType.k_ESteamNetworkingSocketsDebugOutputType_Everything, debugFunc);
            
            #endif
            #if ICE_DISABLED
                int disabled = 0;
                IntPtr ptr = Marshal.AllocHGlobal(sizeof(int));
                Marshal.WriteInt32(ptr, disabled);
                int penalty = 100; // extremely high penalty to discourage direct routes
                Debugger.Print($"ICE Disabled: {SteamNetworkingUtils.SetConfigValue(
                    ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable,
                    ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global,
                    IntPtr.Zero,
                    ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                    ptr
                )}");
                Marshal.FreeHGlobal(ptr);
                SteamNetworkingUtils.SetConfigValue(
                    ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_LocalVirtualPort,
                    ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Global,
                    IntPtr.Zero,
                    ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32,
                    0
                );
            #endif
            SocketStatusCallback = Callback<SocketStatusCallback_t>.Create(SocketStatusFunc);

            online = SteamUser.BLoggedOn();
            steamID = SteamUser.GetSteamID();
            steamName = SteamFriends.GetPersonaName();
            // Yes all it would take to pirate the game is to change the AppId.
            // I don't really care enough to add drm
            // If you pirate an indie game like this, either you couldn't afford it
            // or you're scum
            var owned = SteamApps.BIsSubscribedApp((AppId_t)AppId);
            if (!owned)
            {
                Debugger.Print("User does not own this game");
                // ------------------------------
                // Force quit here
                // ------------------------------
                QuitGame();
            }
            SteamNetworkingUtils.InitRelayNetworkAccess();
            #if ICE_DISABLED
                SteamNetworkingConfigValue_t v = new();
                v.m_eDataType = ESteamNetworkingConfigDataType.k_ESteamNetworkingConfig_Int32;
                v.m_eValue = ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable;
                SteamNetworkingConfigValue_t.OptionValue n = new();
                n.m_int32 = 0;
                v.m_val = n;
                for(int i = 0; i < NUM_SOCKETS; i++){
                    sockets[i] = SteamNetworkingSockets.CreateListenSocketP2P(i, 1, [v]);

                }
            #else
                for (int i = 0; i < NUM_SOCKETS; i++){
                    sockets[i] = SteamNetworkingSockets.CreateListenSocketP2P(i, 0, null);
                }
            #endif
            for (int i = 0; i < NUM_SOCKETS; i++){
                if(sockets[i] == HSteamListenSocket.Invalid){
                    Debugger.PrintErr($"Networking Init: Invalid socket {i}");
                }
            }
            #if ICE_DISABLED
                IntPtr result = Marshal.AllocHGlobal(sizeof(int));
                ulong cbresult = sizeof(int);
                    ESteamNetworkingConfigDataType d;
                    // int outSize = sizeof(long);
                    var readResult = SteamNetworkingUtils.GetConfigValue(
                        ESteamNetworkingConfigValue.k_ESteamNetworkingConfig_P2P_Transport_ICE_Enable,
                        ESteamNetworkingConfigScope.k_ESteamNetworkingConfig_Connection,
                        new IntPtr((long)sockets[0].m_HSteamListenSocket),
                        out d,
                        result,
                        ref cbresult
                    );
                int outValue = Marshal.ReadInt32(result);
                Marshal.FreeHGlobal(result);
                Debugger.Print($"ICE config read result: {readResult}, value: {outValue}");
            #endif

            connectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(NetworkConnectionStatusChanged);
            lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(LobbyChatUpdate);
            lobbyEnterCallback = Callback<LobbyEnter_t>.Create(LobbyJoined);
            joinReqCallback = Callback<GameLobbyJoinRequested_t>.Create(JoinRequested);
            lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(LobbyCreated);
            p2pSessionCallback = Callback<P2PSessionRequest_t>.Create(P2PReq);
            personaStateChangeCallback = Callback<PersonaStateChange_t>.Create(PersonaStateChange);
            relayNetworkStatusCallback = Callback<SteamRelayNetworkStatus_t>.Create(RelayChange);
            ChannelTypePacket.ChannelTypePacketReceived += SetConnectionType;
            Debugger.Print($"NetworkingV2 initialized, user = {steamID}");
            // Debugger.Print("Steamnetworking ready");
            isInit = true;
            #if THREADED_NET
            // If we are threaded, spin up a thread and connect that to Tick()
                ticker = new(ThreadFunc);
                ticker.Start();
            #else
            // Otherwise create a node and attach myself to the process func
                ticker = new(Tick);
                root.CallDeferred("add_child", ticker);
            #endif

        }

        private static void RelayChange(SteamRelayNetworkStatus_t param)
        {
            Debugger.Print($"Relay network status: {param.m_eAvail}");
            Debugger.Print($"Relay network avail: {param.m_eAvailAnyRelay}");
            Debugger.Print($"Relay network config: {param.m_eAvailNetworkConfig}");
            Debugger.Print($"Relay status callback: {param.m_debugMsg}");
        }

        private static void debugFunc(ESteamNetworkingSocketsDebugOutputType nType, StringBuilder pszMsg)
        {
            Debugger.Print($"Type: {nType}\nOutput: {pszMsg}");
        }

        private static void SocketStatusFunc(SocketStatusCallback_t param)
        {
            Debugger.Print($"Socket Status: {param.m_eSNetSocketState}");
        }
#if THREADED_NET
        private static void ThreadFunc()
        {
            Stopwatch sw = new();
            const double targetTickTimeMs = 1000.0 / NetworkingV2.TICKS_PER_SECOND;
            while (!connectionCallerRunning.IsCancellationRequested)
            {
                // Debugger.Print("Calling tick");
                sw.Restart();
                Tick();
                double elapsed;
                while ((elapsed = sw.Elapsed.TotalMilliseconds) < targetTickTimeMs)
                {
                    double remaining = targetTickTimeMs - elapsed;

                    // If we have more than 2ms left, give a small sleep to be CPU friendly
                    if (remaining > 2.0)
                    {
                        Thread.Sleep(1);
                    }
                    else if (remaining > 0.1)
                    {
                        // Yield lets other threads work but stays "warm" for an immediate wake
                        Thread.Yield();
                        break;
                    }
                }
            }
        }
#endif
        private static void Tick(){
            // Debugger.Print("Ticker running");
            OnTick?.Invoke();
            SteamAPI.RunCallbacks();

            // Debugger.Print($"Dequeueing {packetQueue.Count} packets this tick");
            while(packetQueue.TryDequeue(out var packet)){
                // if(packet.data[0] == )
                Debugger.Print($"Deserializing a packet {packet.data[0]} {packet.data[1]} {packet.data[2]}");
                DeserializePacket(packet.data, packet.connection);
            }
            // Debugger.Print($"Queue count: {packetQueue.Count}");
            // Debugger.Print(SteamNetworkingSockets.GetConnectionRealTimeStatus())
        }

        private static void PersonaStateChange(PersonaStateChange_t param)
        {
            // Do nothing for now tbh
        }

        private static void P2PReq(P2PSessionRequest_t param)
        {
            Debugger.Print("P2P session request from: " + param.m_steamIDRemote);

        }
        public static CSteamID GetLobbyOwner(){
            if(lobby.isOwner){
                return steamID;
            } else {
                return SteamMatchmaking.GetLobbyOwner(lobby.lobbyId);
            }
        }
        public static bool IsLobbyOwner(){
            if(lobby == null){
                return true;
            }
            else {
                return lobby.isOwner;
            }
        }
        private static void LobbyCreated(LobbyCreated_t param)
        {
            Debugger.Print($"Lobby created: {param.m_eResult}");
            // Debugger.Print(param.m_eResult);
            if (param.m_eResult == EResult.k_EResultOK){
                if((bool)!lobby?.isOwner)
                {
                    lobby?.LeaveLobby();
                    if(lobby == null){
                        lobby = new((CSteamID)param.m_ulSteamIDLobby);
                    } else {
                        lobby.JoinLobby((CSteamID)param.m_ulSteamIDLobby);
                    }
                    // lobby = new((CSteamID)param.m_ulSteamIDLobby);
                }

                // lobbyId = (CSteamID)param.m_ulSteamIDLobby;
                // Debugger.Print(lobbyId + ", created");
                // displayMessage("Created Lobby: " + lobbyNameEditor.Text);
                // SteamMatchmaking.SetLobbyData(lobbyId, "name", "NoName");
                // var name = SteamMatchmaking.GetLobbyData(lobbyId, "name");
                // chatTitle.Text = name;
                // getLobbyMembers();
                // Steam.AllowP2PPacketRelay(true);
                // addLobbyMember(SteamNetworking.instance.steamId);
            }
        }

        private static void JoinRequested(GameLobbyJoinRequested_t param)
        {
            Debugger.Print("Join requested");
            JoinLobby(param.m_steamIDLobby);
            // throw new NotImplementedException();
        }

        private static void LobbyJoined(LobbyEnter_t param)
        {
            Debugger.Print($"Lobby succesfully joined: {param.m_ulSteamIDLobby}");
            lobby.SetLobbyId(param.m_ulSteamIDLobby);
            SteamMatchmaking.SetLobbyMemberData(lobby.lobbyId, "name", SteamFriends.GetPersonaName());
            if(!lobby.isOwner){
                lobby.CreateMemberList();
            }
        }

        private static void LobbyChatUpdate(LobbyChatUpdate_t param)
        {
            // This name is weird, but it essentially calls on any lobby based packet.
            // For our case this is player joining or leaving etc
            // CSteamID lobbyID = new(param.m_ulSteamIDLobby);
            CSteamID changer = new(param.m_ulSteamIDUserChanged);
            EChatMemberStateChange chatState = (EChatMemberStateChange)param.m_rgfChatMemberStateChange;
            Debugger.Print($"Lobby chat update happened {chatState}");

            // var changer = Steam.GetFriendPersonaName((ulong)makingChangeId);
            // var strChanger = SteamFriends.GetPlayerNickname(changer);
            if(lobby != null){
                // Debugger.Print("Current lobby isn't null");
                if(chatState.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeEntered) || chatState == EChatMemberStateChange.k_EChatMemberStateChangeEntered){
                    Debugger.Print($"Lobby member has joined, not yet initialized: {changer}");
                    playerJoinedSignal?.Invoke(changer);
                    // lobby.PlayerJoined(changer);
                } else if (chatState.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeLeft) || chatState == EChatMemberStateChange.k_EChatMemberStateChangeLeft){
                    lobby.PlayerLeft(changer);
                    playerLeftSignal?.Invoke(changer);
                } else if (chatState.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) || chatState == EChatMemberStateChange.k_EChatMemberStateChangeDisconnected){
                    lobby.PlayerLeft(changer);
                    playerLeftSignal?.Invoke(changer);
                } else if (chatState.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeKicked) || chatState == EChatMemberStateChange.k_EChatMemberStateChangeKicked){
                    lobby.PlayerLeft(changer);
                    playerLeftSignal?.Invoke(changer);

                } else if (chatState.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeBanned) || chatState == EChatMemberStateChange.k_EChatMemberStateChangeBanned){
                    lobby.PlayerLeft(changer);
                    playerLeftSignal?.Invoke(changer);

                } else {
                    // I dont even know what to do in this case
                    Debugger.Print($"Other chat update not yet set: {chatState}");
                }
            } else {
                Debugger.Print("Current lobby is null and we are receiving a chat update");
            }
        }
        public static void AddUnboundSocket(HSteamNetConnection socket){
            lobby.unboundSockets.Add(socket);
        }
        public static void ReceivePacket(ref IntPtr data, ConnectionManager connection){
            // Debugger.Print("Received packet");
            var pkt = Marshal.PtrToStructure<SteamNetworkingMessage_t>(data);
            var length = pkt.m_cbSize;
            var packet = pkt.m_pData;
            byte[] managedData = new byte[length];
            Marshal.Copy(packet, managedData, 0, length);
            packetQueue.Enqueue(new()
            {
                data = managedData,
                connection = connection
            });
            SteamNetworkingMessage_t.Release(data);
        }
        private static void NetworkConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
        {
            // This is called to handle connection acknowledgements.
            // This should not have to be changed at all.
            // Debugger.Print($"Connection status changed: {param.m_info.}");
            switch (param.m_info.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                    Debugger.Print($"Incoming connection from {param.m_info.m_identityRemote.GetSteamID64()}");

                    if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
                    {
                        Debugger.Print($"Connection status changed for {param.m_hConn}");
                        if(lobby.unboundSockets.Contains(param.m_hConn)){
                            Debugger.Print("Found new unbound socket");
                        // 	// We are already dealing with this socket, ignore and remove the unbound connection
                            lobby.unboundSockets.Remove(param.m_hConn);
                            return;
                        }
                        if(param.m_hConn == HSteamNetConnection.Invalid){
                            Debugger.Print("Trying to connect to invalid");
                            return;
                        }
                        EResult result = SteamNetworkingSockets.AcceptConnection(param.m_hConn);
                        if (result == EResult.k_EResultOK)
                        {
                            // var lobbyMember = lobby.GetLobbyMemberById(param.m_info.m_identityRemote.GetSteamID());
                            var cnxn = new ConnectionManager(param.m_hConn);
                            // connectionManagers.Add(cnxn);
                            // lobbyMember.createConnectionListener(param.m_hConn);
                            Debugger.Print("Connection accepted.");
                        }
                        else
                        {
                            Debugger.Print($"Failed to accept connection: {result}");
                            SteamNetworkingSockets.CloseConnection(param.m_hConn, 0, "Failed to accept", false);
                        }
                    }
                    else
                    {
                        // Debugger.PrintErr($"Invalid state for accepting connection: {param.m_info.m_eState}");
                    }
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    break;
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                    Debugger.Print($"Closed by peer, {param.m_info.m_eEndReason} - debug {param.m_info.m_szEndDebug}");
                    SteamNetworkingSockets.CloseConnection(param.m_hConn, 0, null, false);
                    break;
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    Debugger.Print($"Connection closed. Reason: {param.m_info.m_eEndReason} - debug: {param.m_info.m_szEndDebug}");
                    SteamNetConnectionRealTimeStatus_t status = new SteamNetConnectionRealTimeStatus_t();
                    SteamNetConnectionRealTimeLaneStatus_t laneStatus = new();
                    SteamNetworkingSockets.GetConnectionRealTimeStatus(param.m_hConn, ref status, 0, ref laneStatus);
                    Debugger.Print($"Sent: {status.m_cbPendingUnreliable} bytes pending.");
                    Debugger.Print($"Queue: {status.m_cbPendingReliable} bytes reliable.");
                    Debugger.Print($"In-Flight: {status.m_nSendRateBytesPerSecond} Bps");
                    SteamNetworkingSockets.CloseConnection(param.m_hConn, 0, null, false);

                    break;

                default:
                    Debugger.Print($"Connection state changed to {param.m_info.m_eState}, {param.m_info.m_eEndReason}");
                    break;
            }
        }

        public static void SendPacket<T>(ConnectionManager connection, IPacket<T> packet, bool reliable = false) where T : IPacket<T>{
            if(reliable){
                connection.SendPacketReliable(packet);
            } else {
                // Debug only send player packets
                connection.SendPacketUnreliable(packet);
            }
        }
        public static List<LobbyMemberV3> GetLobbyMembers(){
            if(lobby == null){
                // Debugger.Print("Lobby was null yet we were asking for lobby members?");
                return null;
            } else {
                return lobby.lobbyMembers;
            }
        }
        public static void SendPacketToAll<T>(IPacket<T> packet, bool reliable = false, byte channel = 0, bool individualPacket = false) where T : IPacket<T>{
            // Debugger.Print("Sending a packet to everyone");
            if (!isInit || lobby == null)
            {
                return;
            }
            foreach(var player in lobby?.lobbyMembers){
                if(player.steamID != steamID){
                    // Debugger.Print($"Sending packet to {player.memberName}");
                    var cnxn = player.connections[channel];
                    if(reliable){
                        cnxn?.SendPacketReliable(packet);
                    } else {
                        cnxn?.SendPacketUnreliable(packet, individualPacket);

                    }
                }
            }
        }
        public static void JoinLobby(CSteamID lobbyId){
            if(lobby == null){
                lobby = new(lobbyId);
            } else {
                lobby.LeaveLobby();
                lobby.JoinLobby(lobbyId);
            }
        }
        public static void LeaveLobby(){
            lobby?.LeaveLobby();
        }
        public static void CreateLobby(){
            if(lobby == null){
                lobby = new();
            } else {
                lobby.LeaveLobby();
                // connectionManagers = new();
                lobby.CreateLobby();
            }
        }
        public static CSteamID GetLobbyID(){
            if (lobby == null){
                return (CSteamID)0;
            }
            return lobby.lobbyId;
        }
        public static void SetConnectionType(ChannelTypePacket packet, ConnectionManager connection){
            var id = packet.id;
            var type = packet.cType;
            var member = lobby.GetLobbyMemberById(id);
            if(member == null){
                Debugger.Print("Lobby member not set up for some reason");
                return;
            }
            connection.SetSteamId(id);
            // Debugger.Print($"{type}");
            for (byte i = 0; i < NUM_SOCKETS; i++){
                if(type == i){
                    if (member.connections[i] != null)
                    {
                        #if NET_DEBUG
                            Debugger.Print("Connection already established skipping");
                        #endif
                    }
                    member.connections[i] = connection;
                    bool shouldStart = true;
                    for (byte j = 0; j < NUM_SOCKETS; j++){
                        if(member.connections[j] == null){
                            shouldStart = false;
                            break;
                        }
                    }
                    if(shouldStart && IsLobbyOwner()){
                        StartGame(connection); // This isn't going to be null checked because it *SHOULD* fail if it is null
                    }
                }
            }
        }
        public static void LeaveGame(){
            if(started){
                started = false;
                lobby.LeaveLobby();
            }
        }

        internal static void ShutdownThread()
        {
#if THREADED_NET
            connectionCallerRunning.Cancel();
#else
            ticker.QueueFree();
#endif
        }
    }
}
