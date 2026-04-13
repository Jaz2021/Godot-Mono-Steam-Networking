// using System.Collections.Generic;
// using Steamworks;
// using Godot;

// namespace Networking_V2
// {
//     public class LobbyMemberV2
//     {
//         public LobbyMemberV2(CSteamID steamID, string name, bool shouldEstablishConnections){
//             Debugger.Print($"Lobby member object created {steamID}:{name}, should establish connections: {shouldEstablishConnections}");
//             this.steamID = steamID;
//             memberName = name;
//             if(shouldEstablishConnections){
//                 EstablishConnections();
//             }
//             ResetName();
//         }
//         public ConnectionManager gameplayConnection;
//         public ConnectionManager audioConnection;
//         public CSteamID steamID {
//             get;
//         }
//         public string memberName {
//             private set;
//             get;
//         }
//         public void ResetName(){
//             memberName = SteamMatchmaking.GetLobbyMemberData(NetworkingV2.GetLobbyID(), steamID, "name");
//             Debugger.Print($"Reset lobby member: {steamID}'s name to {memberName}");
//         }
//         public void ClearConnections(){
//             Debugger.Print("Dropping connections");
//             gameplayConnection?.DropConnection();
//             gameplayConnection = null;
//             audioConnection?.DropConnection();
//             audioConnection = null;
//         }
//         private void EstablishConnections(){
//             Debugger.Print("Establishing connections");
//             SteamNetworkingIdentity netId = new();
//             netId.SetSteamID(steamID);
//             gameplayConnection = new(netId, ChannelTypePacket.ChannelType.Gameplay);
//             audioConnection = new(netId, ChannelTypePacket.ChannelType.Audio);
//         }
//     }
// }