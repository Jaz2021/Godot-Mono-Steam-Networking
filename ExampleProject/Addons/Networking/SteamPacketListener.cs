using Godot;
namespace Networking_V2{
    public partial class SteamPacketListener : Node {
        // Thats it. Thats all this thing does
        public override void _EnterTree()
        {
            GD.Print("Steam packet listener created");
        }
        public override void _Process(double delta)
        {
            // NetworkingV2.ListenForPackets();
        }
        public override void _ExitTree()
        {
            GD.Print("Steam packet listener destroyed");
        }
    }
}
