# Godot Mono Steam Networking

Godot Mono Steam Networking is a Godot addon designed to make Steamworks-based multiplayer networking easier and more modular. It integrates with an external C# packet code generator (`NetworkingV2Generator`) and supports lobby creation, packet sending, and a flexible system for defining new packet types.

## Project Structure

Your directory structure **must** be as follows:

```
/Main Directory
│
├── Your Godot Project/    # All the files for your godot project, including the scripts in the base project
├── NetworkingV2Generator/ # The external packet code generator (sibling to the Godot project)
```

> The `NetworkingV2Generator` folder must be in the same folder as your Godot project folder.

---

## Setup

1. **Add the scripts** to your Godot project
2. **Download and include the Steamworks SDK** in your system or directly into your project:
   - You can get it from: https://partner.steamgames.com/doc/sdk
3. Ensure `NetworkingV2Generator` is correctly located as described above. You may have to move your godot project one folder further in for git.

---

## Initialization

Before using any networking features, initialize the system:

``csharp
NetworkingV2.Init(force: false);
``

- `force` (`bool`): If `true`, the game will automatically quit if initialization fails (e.g., Steam is not running).

---

## Creating a Lobby

To create a new multiplayer lobby:

``csharp
NetworkingV2.CreateLobby();
``

- If the player is already in a lobby, this will automatically leave it before creating a new one.

---

## Sending Packets

To send a packet to a specific connection:

``csharp
NetworkingV2.SendPacket(connection, packet, reliable);
``

- `connection`: Target connection manager.
- `packet`: The packet object to send, must inherit IPacket.
- `reliable`: If `true`, uses a slower but guaranteed delivery method.

To send to **all players** in the lobby (either on gameplay or audio channels):

``csharp
NetworkingV2.SendPacketToAll(packet, reliable);
``

---

## Adding New Packet Types

Creating new packet types is simple. Just define a new class like this:

``csharp
[Packet(0)]
public class MyCustomPacket : IPacket<MyCustomPacket>
{
    // Your fields and methods here
}
``

### Notes:
- Each packet must implement `IPacket<T>`.
- The `[Packet(x)]` attribute registers the packet with a unique ID (`x`).
- The unique ID **must** be the first 3 bytes in the packet. These three bytes are used as redundancy to make it incredibly unlikely for a packet to be misinterpreted.
- It is **recommended** to include a static delegate (event) in each packet type for handling incoming packets:

``csharp
public delegate void MyPacketSignal(MyPacket packet, ConnectionManager from);
public static MyPacketSignal myPacketSignal;
``

- You can then subscribe to `Signal` from any object to handle specific packet types.

> Handling and signaling logic is left to the developer to implement, giving you full flexibility.

---

## Dependencies

- Godot (C# support)
- Steamworks SDK
- `NetworkingV2Generator` (external C# code generator)

---

## Features

- Steam lobby creation
- Reliable and unreliable packet sending
- Code-first packet registration and processing
- Flexible signal-based packet handling
- Easily extensible with custom packet types

---

## License

MIT License (or add your own license here)

---

## Support

Open an issue or submit a pull request if you'd like to contribute or need help.
