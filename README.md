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

Creating new packet types is simple. Just define a new class like this, with all serialized data given a `[SerializeData]` attribute:

``csharp
[Packet]
public class MyCustomPacket : IPacket<MyCustomPacket>
{
    // Your fields heres
    // For example:
    [SerializeData]
    public int IntegerData;
}
``
### Adding New Serializable Data Types:

### Notes:
- Each packet must implement `IPacket<T>`.
- The `[Packet]` attribute registers the packet with a unique ID according to alphabeical order.
- The `[Packet]` attribute can be overloaded to pick and choose what functions are automatically generated
- The `[SerializeData]` attribute assumes that the type in question has an extension method named `.Serialize()` and a function in PtrConverter named `.GetType`.
- The `[SerializeData]` attribute **does not** work on types with a template or arrays, if you would like to use such, implement a container class to hold them as well as a Serialize() and PtrConverter.Get function.
- There is currently no size checking implemented, this could theoretically read out of bounds memory and crash whatever project you are working on.

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

## TODO

- Implement a size checking on a per packet type basis.
- - It would be best to have a way to dynamically read packet size for certain packets.

## License

MIT License (or add your own license here)

---

## Support

Open an issue or submit a pull request if you'd like to contribute or need help.
