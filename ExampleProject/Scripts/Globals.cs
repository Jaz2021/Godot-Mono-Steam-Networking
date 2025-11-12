using Godot;
using Networking_V2;
using Steamworks;
using System;
using System.Collections.Generic;

public partial class Globals : Node
{
	public static Globals Instance {
		get;
		private set;
	}
	[Export] public Node SceneRoot;
	[Export] public CanvasLayer MenuRoot;
	[Export] public CameraController PlayerCam;
	[Export] public Node EnvironmentRoot;
	[Export] private PackedScene InitialMenu;
	[Export] private PackedScene GameplayScene;
	[Export] private PackedScene PauseMenu;
	private Scene currentScene = null;
	private MenuController currentMenu = null;
	private Env currentEnv = null;
	private bool inGame = false;
	// No initial scene, that will get loaded once the player starts the game
	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			GD.PrintErr("Error, tried to recreate globals. This script should only exist on the outermost node in the tree");
			QueueFree();
		}
		ChangeMenu(InitialMenu);
		NetworkingV2.Init(false);
		StartGamePacket.StartGamePacketReceived += StartOnlineGame;
	}
	public void SendStartGamePacket(ConnectionManager connection)
	{
		List<ObjectStruct> objStructs = new();
		foreach(var obj in currentScene.objects)
        {
			objStructs.Add(new(obj.id, obj.Position, obj.Rotation, obj.type));
        }
		ObjectStructList objs = new(objStructs.ToArray());
		StartGamePacket packet = new(objs, Vector3.Zero, Vector3.Zero); // Temporarily setting 0,0 as the start position
		GD.Print("Sending start game packet");
		NetworkingV2.SendPacket(connection, packet, true);
		SpawnObjectPacket objPacket = new((ulong)connection.steamID, (byte)ObjectType.Player, Vector3.Zero, Vector3.Zero);
		NetworkingV2.SendPacketToAll(objPacket, true);
		SpawnObjectPacket.SpawnObjectPacketReceived(objPacket, null);
    }
	public void StartOnlineGame(StartGamePacket packet, ConnectionManager cnxn)
	{

		NetworkingV2.Init(true);
		if (inGame)
		{
			return; // Ignore an extra start game packet
		}
        if (NetworkingV2.isInit)
        {
            
        }
		if ((ulong)NetworkingV2.GetLobbyID() == 0 && cnxn == null)
		{
			// We got startgame from ourself and there is no lobby yet.
			NetworkingV2.CreateLobby();
			ChangeScene(GameplayScene, packet.StartPosition, packet.StartRotation);
			ChangeMenu(PauseMenu);
			return;
		}
		if ((cnxn == null && NetworkingV2.IsLobbyOwner()) || cnxn.steamID == NetworkingV2.GetLobbyOwner())
		{
			// We got the startgame from either ourselves or the lobby owner
			ChangeScene(GameplayScene, packet.StartPosition, packet.StartRotation, packet.ExistingObjects);
			ChangeMenu(PauseMenu);
			return;
		}
	}
	public void StartOfflineGame()
    {
		ChangeScene(GameplayScene, Vector3.Zero, Vector3.Zero);
		ChangeMenu(PauseMenu);
    }
	public override void _ExitTree()
	{
		GD.PrintErr("Warning, Globals was freed, something has probably gone wrong");
		Instance = null;
	}
	public void ChangeScene(PackedScene newScene, Vector3 position, Vector3 rotation, ObjectStructList objs = null)
	{
		// All scenes must have a parent node that implements IScene.
		currentScene?.QueueFree();
		if(newScene != null)
		{
			currentScene = newScene.Instantiate<Scene>();
			SceneRoot.AddChild(currentScene);
			currentScene.EnterScene(objs, position, rotation);
			PlayerCam.Visible = true;
		} else
        {
			PlayerCam.Visible = false;
        }
	}
	public void ChangeMenu(PackedScene newMenu)
	{
		// All menus must have a parent node that implements MenuController
		currentMenu?.QueueFree();
		currentMenu = newMenu.Instantiate<MenuController>();
		MenuRoot.AddChild(currentMenu);
		currentMenu.EnterMenu();
	}
	public void ChangeEnvironment(PackedScene newEnvironment)
	{
		// All environments must have a parent node that implements IEnv
		currentEnv?.QueueFree();
		currentEnv = newEnvironment.Instantiate<Env>();
		currentEnv.Enter();
		EnvironmentRoot.AddChild(currentEnv);
	}
    public override void _Process(double delta)
    {
        if (NetworkingV2.isInit)
        {
			SteamAPI.RunCallbacks();
        }
    }
}
