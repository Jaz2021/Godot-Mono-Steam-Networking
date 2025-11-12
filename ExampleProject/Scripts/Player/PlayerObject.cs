using Godot;
using Networking_V2;
public partial class PlayerObject : NetObject
{

    public override void _Ready()
    {
        PlayerPacket.PlayerPacketReceived += PlayerPacketReceived;
    }
    private void PlayerPacketReceived(PlayerPacket packet, ConnectionManager connection)
    {
        // GD.Print($"Received player packet with id: {packet.id}, my id: {id}");
        if (connection != null)
        {
            if (id == (ulong)NetworkingV2.steamID && connection.steamID != NetworkingV2.GetLobbyOwner())
            {
                // If you are receiving for your own player and it is coming from not the lobby owner, ignore it
                return;
            }
        }
        // Received packet from myself or the lobby owner, so listen to it
        if (id == packet.id)
        {
            pos = packet.position;
            angVel = packet.angularVelocity;
            vel = packet.velocity;
            rot = packet.rotation;
            ReceivedUpdate = true;
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        Grounded = IsOnGround();
        if(id == (ulong)NetworkingV2.steamID)
        {
            // GD.Print("Sending player object packet");
            // We have our own steam id, therefore we can send our packet of data.
            PlayerPacket packet = new(AngularVelocity, id, RealPosition, GlobalRotation, LinearVelocity);
            NetworkingV2.SendPacketToAll(packet);
        }
    }

}
