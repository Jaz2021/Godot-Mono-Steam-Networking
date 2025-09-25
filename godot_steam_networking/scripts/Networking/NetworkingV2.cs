using System;
using System.Collections.Generic;
using System.Resources;
using System.Runtime.InteropServices;
using Godot;
using Microsoft.VisualBasic;
using Steamworks;


namespace Networking_V2{
    public static partial class NetworkingV2 {
        private static SteamLobby lobby;
        private const int AppId = 480;
        public const int MTU = 1200;
        public static bool isInit
        {
            private set;
            get;
        } = false;
        private static bool online = false;
        public static CSteamID steamID { private set; get; }
        private static string steamName = "";
        private static HSteamListenSocket gameplaySocket;
        private static HSteamListenSocket audioSocket;
        private static Callback<SteamNetConnectionStatusChangedCallback_t> connectionStatusChangedCallback;
        private static Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;
        private static Callback<LobbyEnter_t> lobbyEnterCallback;
        private static Callback<GameLobbyJoinRequested_t> joinReqCallback;
        private static Callback<LobbyCreated_t> lobbyCreatedCallback;
        private static Callback<P2PSessionRequest_t> p2pSessionCallback;
        private static Callback<PersonaStateChange_t> personaStateChangeCallback;
        public delegate void PlayerDelegate(CSteamID player);
        public delegate void PlayerReadyDelegate(ConnectionManager gameplayCnxn, ConnectionManager audioCnxn);
        public static PlayerDelegate playerJoinedSignal;
        public static PlayerDelegate playerLeftSignal;
        public static PlayerReadyDelegate playerReadySignal;
        private static bool started = false;
        public static void Init(bool force = false){
            if(isInit){
                GD.Print("Networking already initialized, don't need to reinit");
                return;
            }
            var init = SteamAPI.Init();
            if(!init){
                GD.Print("Failed to initialize steam");
                if (force)
                {
                    GD.Print("Shutting down...");
                    // --------------------------------
                    // Quit the game here
                    // --------------------------------
                    
                }
            }
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
                GD.Print("User does not own this game");
                // ------------------------------
                // Force quit here
                // ------------------------------
            }
            SteamNetworkingUtils.InitRelayNetworkAccess();
            gameplaySocket = SteamNetworkingSockets.CreateListenSocketP2P(0, 0, []);
            if(gameplaySocket == HSteamListenSocket.Invalid){
                GD.Print("V2: Invalid gameplay socket");
            }
            audioSocket = SteamNetworkingSockets.CreateListenSocketP2P(1, 0, []);
            if(audioSocket == HSteamListenSocket.Invalid){
                GD.Print("V2: Invalid audio socket");
            }
            
            connectionStatusChangedCallback = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(NetworkConnectionStatusChanged);
            lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(LobbyChatUpdate);
            lobbyEnterCallback = Callback<LobbyEnter_t>.Create(LobbyJoined);
            joinReqCallback = Callback<GameLobbyJoinRequested_t>.Create(JoinRequested);
            lobbyCreatedCallback = Callback<LobbyCreated_t>.Create(LobbyCreated);
            p2pSessionCallback = Callback<P2PSessionRequest_t>.Create(P2PReq);
            personaStateChangeCallback = Callback<PersonaStateChange_t>.Create(PersonaStateChange);
            GD.Print($"NetworkingV2 initialized, user = {steamID}");
            // GD.Print("Steamnetworking ready");
            isInit = true;
        }

        private static void PersonaStateChange(PersonaStateChange_t param)
        {
            
        }

        private static void P2PReq(P2PSessionRequest_t param)
        {
            GD.Print("P2P session request from: " + param.m_steamIDRemote);

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
                return false;
            }
            else {
                return lobby.isOwner;
            }
        }
        private static void LobbyCreated(LobbyCreated_t param)
        {
            GD.Print($"Lobby created {param.m_eResult}");
            // GD.Print(param.m_eResult);
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
                // GD.Print(lobbyId + ", created");
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
            GD.Print("Join requested");
            JoinLobby(param.m_steamIDLobby);
            // throw new NotImplementedException();
        }

        private static void LobbyJoined(LobbyEnter_t param)
        {
            GD.Print($"Lobby succesfully joined: {param.m_ulSteamIDLobby}");
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
            GD.Print($"Lobby chat update happened {chatState}");

            // var changer = Steam.GetFriendPersonaName((ulong)makingChangeId);
            // var strChanger = SteamFriends.GetPlayerNickname(changer);
            if(lobby != null){
                // GD.Print("Current lobby isn't null");
                if(chatState.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeEntered) || chatState == EChatMemberStateChange.k_EChatMemberStateChangeEntered){
                    GD.Print($"Lobby member has joined, not yet initialized: {changer}");
                    playerJoinedSignal?.Invoke(changer);
                    // lobby.PlayerJoined(changer);
                } else if (chatState.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeLeft) || chatState == EChatMemberStateChange.k_EChatMemberStateChangeLeft){
                    // lobby.PlayerLeft(changer);
                    playerLeftSignal?.Invoke(changer);
                } else if (chatState.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) || chatState == EChatMemberStateChange.k_EChatMemberStateChangeDisconnected){
                    // lobby.PlayerLeft(changer);
                    playerLeftSignal?.Invoke(changer);
                } else if (chatState.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeKicked) || chatState == EChatMemberStateChange.k_EChatMemberStateChangeKicked){
                    // lobby.PlayerLeft(changer);
                    playerLeftSignal?.Invoke(changer);

                } else if (chatState.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeBanned) || chatState == EChatMemberStateChange.k_EChatMemberStateChangeBanned){
                    // lobby.PlayerLeft(changer);
                    playerLeftSignal?.Invoke(changer);

                } else {
                    // I dont even know what to do in this case
                    GD.Print($"Other chat update not yet set: {chatState}");
                }
            } else {
                GD.Print("Current lobby is null and we are receiving a chat update");
            }
        }
        public static void AddUnboundSocket(HSteamNetConnection socket){
            lobby.unboundSockets.Add(socket);
        }
        private static void NetworkConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
        {
            // This is called to handle connection acknowledgements.
            // This should not have to be changed at all.

            switch (param.m_info.m_eState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                    GD.Print($"Incoming connection from {param.m_info.m_identityRemote.GetSteamID64()}");

                    if (param.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
                    {
                        GD.Print($"Connection status changed for {param.m_hConn}");
                        if(lobby.unboundSockets.Contains(param.m_hConn)){
                            GD.Print("Found new unbound socket");
                        // 	// We are already dealing with this socket, ignore and remove the unbound connection
                            lobby.unboundSockets.Remove(param.m_hConn);
                            return;
                        }
                        if(param.m_hConn == HSteamNetConnection.Invalid){
                            GD.Print("Trying to connect to invalid");
                            return;
                        }
                        EResult result = SteamNetworkingSockets.AcceptConnection(param.m_hConn);
                        if (result == EResult.k_EResultOK)
                        {
                            // var lobbyMember = lobby.GetLobbyMemberById(param.m_info.m_identityRemote.GetSteamID());
                            var _ = new ConnectionManager(param.m_hConn);
                            // lobbyMember.createConnectionListener(param.m_hConn);
                            GD.Print("Connection accepted.");
                        }
                        else
                        {
                            GD.Print($"Failed to accept connection: {result}");
                            SteamNetworkingSockets.CloseConnection(param.m_hConn, 0, "Failed to accept", false);
                        }
                    }
                    else
                    {
                        // GD.PrintErr($"Invalid state for accepting connection: {param.m_info.m_eState}");
                    }
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    break;
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                    GD.Print($"Connection closed. Reason: {param.m_info.m_eEndReason}");
                    SteamNetworkingSockets.CloseConnection(param.m_hConn, 0, null, false);
                    break;

                default:
                    // GD.Print($"Connection state changed to {param.m_info.m_eState}");
                    break;
            }
        }

        public static void SendPacket<T>(ConnectionManager connection, IPacket<T> packet, bool reliable = false) where T : IPacket<T>{
            if(reliable){
                connection.SendPacketReliable(packet);
            } else {
                connection.SendPacketUnreliable(packet);
            }
        }
        public static List<LobbyMemberV2> GetLobbyMembers(){
            if(lobby == null){
                GD.Print("Lobby was null yet we were asking for lobby members?");
                return null;
            } else {
                return lobby.lobbyMembers;
            }
        }
        public static void SendPacketToAll<T>(IPacket<T> packet, bool reliable = false, bool gameplay = true, bool individualPacket = false) where T : IPacket<T>{
            // GD.Print("Sending a packet to everyone");
            foreach(var player in lobby?.lobbyMembers){
                if(player.steamID != steamID){
                    // GD.Print($"Sending packet to {player.memberName}");
                    var cnxn = gameplay ? player.gameplayConnection : player.audioConnection;
                    if(reliable){
                        cnxn?.SendPacketReliable(packet);
                    } else {
                        cnxn?.SendPacketUnreliable(packet, individualPacket);

                    }
                    // if(cnxn == null){
                    //     GD.Print("Connection was null");
                    // }
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
                lobby.CreateLobby();
            }
        }
        public static CSteamID GetLobbyID(){
            if (lobby == null){
                return (CSteamID)0;
            }
            return lobby.lobbyId;
        }
        public static void SetConnectionType(ConnectionManager connection, ChannelTypePacket.ChannelType type, CSteamID id){
            var member = lobby.GetLobbyMemberById(id);
            if(member == null){
                GD.Print("Lobby member not set up for some reason");
                return;
            }
            connection.SetSteamId(id);
            // GD.Print($"{type}");

            switch (type){
                case ChannelTypePacket.ChannelType.Gameplay:
                    // GD.Print($"{type}");
                    if(member.gameplayConnection == null){
                        member.gameplayConnection = connection;
                        member.ResetName();
                        GD.Print($"{member.memberName}'s gameplay connection established");
                        if(member.audioConnection != null){
                            if(IsLobbyOwner()){
                                // ---------------------------------
                                // Send a start game packet here
                                // If you want late joining enabled
                                // ---------------------------------
                            }
                            playerReadySignal?.Invoke(member.gameplayConnection, member.audioConnection);
                            // GD.Print("Player ready sent");
                        }
                    } else {
                        GD.Print("Received a channeltype packet for a connection that we already have");
                    }
                    break;
                case ChannelTypePacket.ChannelType.Audio:
                    // GD.Print($"{type}");
                    if(member.audioConnection == null){
                        member.audioConnection = connection;
                        GD.Print($"{member.memberName}'s audio connection established");
                        member.ResetName();
                        if(member.gameplayConnection != null){
                            if (IsLobbyOwner())
                            {
                                // ---------------------------------
                                // Send a start game packet here
                                // If you want late joining enabled
                                // ---------------------------------
                            }
                            playerReadySignal?.Invoke(member.gameplayConnection, member.audioConnection);
                            // GD.Print("Player ready sent");
                        }
                    } else {
                        GD.Print("Received a channeltype packet for a connection that we already have");
                    }
                    break;
            }
        }
        // public static void StartGame(StartGamePacket packet, CSteamID from)
        // {
        //     if (!started)
        //     {
        //         if (from != GetLobbyOwner())
        //         {
        //             GD.Print("Recieved startgame packet from wrong lobby owner");
        //             return;
        //         }
        //         GD.Print("Starting the game");
        //         started = true;
        //         // ----------------------------------------
        //         // ADD YOUR CODE HERE FOR STARTING THE GAME
        //         // ----------------------------------------
        //     }
        //     else
        //     {
        //         GD.Print("Received start game signal when game already started");
        //     }
        // }
        public static void LeaveGame(){
            if(started){
                started = false;
                lobby.LeaveLobby();
            }
        }
    }
}
