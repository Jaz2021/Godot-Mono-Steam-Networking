using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Godot;
using Networking_V2;
using Steamworks;
public class SteamLobby{
    public CSteamID lobbyId{
        get;
        private set;
    }
    public List<LobbyMemberV2> lobbyMembers = new();
    public List<HSteamNetConnection> unboundSockets = new();
    public bool isOwner = true;
    public SteamLobby(CSteamID lobbyToJoin){
        NetworkingV2.playerJoinedSignal += PlayerJoined;
        NetworkingV2.playerLeftSignal += PlayerLeft;
        Console.Log($"Lobby created to join {lobbyToJoin}");
        JoinLobby(lobbyToJoin);
        // Globals.instance.root.AddChild(this);
    }
    public void SetLobbyId(ulong lobbyId){
        this.lobbyId = (CSteamID)lobbyId;
    }
    public SteamLobby(){
        NetworkingV2.playerJoinedSignal += PlayerJoined;
        NetworkingV2.playerLeftSignal += PlayerLeft;
        Console.Log("Lobby created");
        CreateLobby();
        // Globals.instance.root.AddChild(this);
    }
    ~SteamLobby()
    {
        NetworkingV2.playerJoinedSignal -= PlayerJoined;
        NetworkingV2.playerLeftSignal -= PlayerLeft;
        if(((ulong)lobbyId) != 0){
            Console.Log("Lobby object destroyed without first calling LeaveLobby(). You should figure out what caused that", Console.MessageType.Error);
            SteamMatchmaking.LeaveLobby(lobbyId);
        }
        Console.Log("Lobby object was destroyed", Console.MessageType.Warning);
    }
    public void ResetLobby(){
        foreach(var member in lobbyMembers){
            member.ClearConnections();
        }
        lobbyMembers.Clear();
    }
    public void JoinLobby(CSteamID lobbyToJoin){
        if(lobbyToJoin == lobbyId){
            return; // Early escape
        }
        SteamMatchmaking.JoinLobby(lobbyToJoin);
        lobbyId = lobbyToJoin;
        isOwner = false;
    }
    public void CreateMemberList(){
        for(int i = 0; i < SteamMatchmaking.GetNumLobbyMembers(lobbyId); i++){
            var lobbyMember = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
            // Create a lobby member and do not add connections to myself
            AddLobbyMember(lobbyMember, lobbyMember != NetworkingV2.steamID);
        }
    }
    public LobbyMemberV2 GetLobbyMemberById(CSteamID id){
        foreach(var member in lobbyMembers){
            if(member.steamID == id){
                return member;
            } else {
                // Console.Log($"{id} != {member.steamID}");
            }
        }
        return null;
    }
    private void AddLobbyMember(CSteamID mem, bool shouldCreateConnections = false){
        Console.Log("Adding lobby member: " + mem, Console.MessageType.NetworkingConnections);
        foreach(var member in lobbyMembers){
            // Console.Log(member.MemberName);
            if(member.steamID == mem){
                Console.Log($"Tried to readd a lobby member that already exists: {member.memberName}", Console.MessageType.Error);
                // Console.Log("Tried to readd a lobby member that already exists");
                return;
            }
        }
        var isUser = mem == SteamUser.GetSteamID();
        string name = "";
        if(isUser){
            name = SteamFriends.GetPersonaName();
        } else {
            // name = SteamFriends.GetFriendPersonaName(mem); // This is bad it for some reason never runs
            // SteamFriends.RequestUserInformation(mem, true);
        }
        LobbyMemberV2 newMem = new(mem, name, shouldCreateConnections);
        lobbyMembers.Add(newMem);
        Console.Log($"Added child {lobbyMembers.Count}");
    }
    private void RemoveLobbyMember(CSteamID mem){
        Console.Log($"Member: {mem} left the game");
        int i = 0;
        foreach(var member in lobbyMembers){
            if(mem == member.steamID){
                member.ClearConnections();
                lobbyMembers.RemoveAt(i);
                break;
            }
            i++;
        }
    }
    public void AddUnboundSockets(HSteamNetConnection socket){
        // This exists so that unbound sockets is private. It just exists to help the connection stuff
        unboundSockets.Add(socket);
    }
    public void CreateLobby(){
        Console.Log("Lobby being created", Console.MessageType.Networking);
        if(lobbyId != (CSteamID)0){
            ResetLobby();
        }
        AddLobbyMember(SteamUser.GetSteamID());
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 4);

    }
    public void PlayerJoined(CSteamID member){
		Console.Log("Player joined: " + member, Console.MessageType.NetworkingConnections);
		// var name = SteamFriends.GetFriendPersonaName(member);
        AddLobbyMember(member);
        // PlayerJoinedSignal?.Invoke(member);


    }
    public void LeaveLobby(){
        if (((ulong)lobbyId) == 0){
            return;
        }
        ResetLobby();
        SteamMatchmaking.LeaveLobby(lobbyId);  
        lobbyId = (CSteamID)0;
    }
	public void PlayerLeft(CSteamID member){
		// Globals.instance.removePlayer(member.m_SteamID);
        RemoveLobbyMember(member);
	}
    

}