using Steamworks;

namespace Networking_V2;

public class LobbyMemberV3{
    public LobbyMemberV3(CSteamID steamID, string name, bool shouldEstablishConnections){
        Debugger.Print($"Lobby member object created {steamID}:{name}, should establish connections: {shouldEstablishConnections}");
        this.steamID = steamID;
        memberName = name;
        if(shouldEstablishConnections){
            EstablishConnections();
        }
        ResetName();
    }
    public ConnectionManager[] connections = new ConnectionManager[NetworkingV2.NUM_SOCKETS];
    public CSteamID steamID {
        get;
    }
    public string memberName {
        private set;
        get;
    }
    public void ResetName(){
        memberName = SteamMatchmaking.GetLobbyMemberData(NetworkingV2.GetLobbyID(), steamID, "name");
        Debugger.Print($"Reset lobby member: {steamID}'s name to {memberName}");
    }
    public void ClearConnections(){
        Debugger.Print("Dropping connections");
        for (int i = 0; i < NetworkingV2.NUM_SOCKETS; i++){
            connections[i].DropConnection();
        }
    }
    private void EstablishConnections(){
        Debugger.Print("Establishing connections");
        SteamNetworkingIdentity netId = new();
        netId.SetSteamID(steamID);
        for (byte i = 0; i < NetworkingV2.NUM_SOCKETS; i++)
        {
            connections[i] = new(netId, i);
        }


    }
}