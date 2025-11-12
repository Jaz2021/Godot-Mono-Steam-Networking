using Godot;
using Networking_V2;
using System;

public partial class MainPlayer : Node
{
    private PlayerObject myObj;
    [Export] private Vector3 CameraOffset;
    [Export] private float moveSpeedForward;
    [Export] private float moveSpeedLR;
    [Export] private float sprintMult;
    [Export] private float JumpForce;
    [Export] private float lookSens = 0.001f;
    public float friction
    {
        get
        {
            return myObj.PhysicsMaterialOverride.Friction;
        }
        set
        {
            myObj.PhysicsMaterialOverride.Friction = value;
        }
    }
    private Vector3 moveDir = Vector3.Zero;
    private Vector2 lookDir = Vector2.Zero;
    public void SetPlayerObject(PlayerObject pobj)
    {
        myObj = pobj; // Set the player object that we update every tick
        pobj.GetChild<MeshInstance3D>(0).Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        // myObj.PhysicsMaterialOverride = new();
        // myObj.PhysicsMaterialOverride.Rough = true;
    }

    public override void _Process(double delta)
    {
        if (myObj == null)
        {
            return;
        }
        if(Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            Globals.Instance.PlayerCam.AddRotation(lookDir * lookSens);
        }
        lookDir = Vector2.Zero;
        Globals.Instance.PlayerCam.GlobalPosition = myObj.GlobalPosition + CameraOffset;
        // Handle our inputs
        Vector2 inputDir = Vector2.Zero;
        if (!myObj.Grounded)
        {
            return;
        }
        if (Input.IsActionPressed("MoveForward"))
        {
            inputDir.Y = -1f;
            // moveDir.Z -= moveSpeedForward * (float)delta;
        }
        if (Input.IsActionPressed("MoveBack"))
        {
            inputDir.Y = 1f;
            // moveDir.Z += moveSpeedForward * (float)delta;
        }
        if (Input.IsActionPressed("MoveLeft"))
        {
            inputDir.X = -1f;
            // moveDir.X -= moveSpeedLR * (float)delta;
        }
        if (Input.IsActionPressed("MoveRight"))
        {
            inputDir.X = 1f;
            // moveDir.X += moveSpeedLR * (float)delta;
        }
        

        inputDir = inputDir.Normalized();
        inputDir = new(inputDir.X * (float)delta * moveSpeedLR, inputDir.Y * (float)delta * moveSpeedForward);
        // GD.Print($"Input Dir before: {inputDir}");
        inputDir = inputDir.Rotated(-Globals.Instance.PlayerCam.GetLookDir());
        if (Input.IsActionPressed("Sprint"))    
        {
            inputDir *= sprintMult;
        }
        // GD.Print($"Input Dir after: {inputDir}");
        moveDir += new Vector3(inputDir.X, 0, inputDir.Y);
        // GD.Print($"Movedir: {moveDir}, velocity: {myObj.LinearVelocity}");
        
    }
    public override void _Input(InputEvent e)
    {
        if(e is InputEventMouseMotion m)
        {
            lookDir += m.ScreenRelative;
            return;
        }
        if (e.IsActionPressed("Interact"))
        {
        }
        if (e.IsActionPressed("Pause"))
        {
            if(Input.MouseMode == Input.MouseModeEnum.Captured)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
            } else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionPressed("Jump"))
        {
            // GD.Print("Checking if on ground");
            if (myObj.Grounded && moveDir.Y == 0)
            {
                // GD.Print("Jumping");
                moveDir.Y += JumpForce;
            }
        }
        // myObj.AddConstantCentralForce(moveDir);
        myObj.Rotation = new(0f, Globals.Instance.PlayerCam.GetLookDir(), 0f);
        myObj.ApplyCentralForce(moveDir);
        moveDir = Vector3.Zero;
        SendPacket();
    }
    private void SendPacket()
    {
        if(NetworkingV2.isInit)
        {
            PlayerPacket packet = new(myObj.AngularVelocity, myObj.id, myObj.Position, myObj.Rotation, myObj.AngularVelocity);
            NetworkingV2.SendPacketToAll(packet);
        }
    }
}
