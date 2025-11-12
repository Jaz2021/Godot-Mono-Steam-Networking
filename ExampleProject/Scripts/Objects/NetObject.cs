using Godot;

public partial class NetObject : RigidBody3D
{
    protected bool ReceivedUpdate = false;
    protected Vector3 pos;
    protected Vector3 angVel;
    protected Vector3 vel;
    protected Vector3 rot;
    [Export] private float JumpRayLength; // Debug
    [Export] private float MaxSpeed = 5f;
    [Export] private RayCast3D FootPos; // Suggested to use a Marker3D to show the position
    public ObjectType type
    {
        get;
        private set;
    }
    public Vector3 RealPosition
    {
        get
        {
            return objRoot.Position + Position;
        }
        set
        {
            Position = value - objRoot.Position;
        }
    }
    public ulong id
    {
        get;
        protected set;
    }
    public bool Grounded
    {
        get;
        protected set;
    }
    protected Node3D objRoot;
    public void Init(ulong id, ObjectType type, Node3D objRoot, Vector3 position, Vector3 rotation)
    {
        GD.Print($"Initiallizing NetObj with id {id}");
        this.id = id;
        this.objRoot = objRoot;
        rot = rotation;
        pos = position;
        this.type = type;
        ReceivedUpdate = true;
    }
    public override void _PhysicsProcess(double delta)
    {
        // GD.Print($"{Grounded}");
        Grounded = IsOnGround();
    }
    protected bool IsOnGround()
    {
        FootPos.ForceRaycastUpdate();
        return FootPos.IsColliding() && LinearVelocity.Y <= 0.001f;
    }
    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (ReceivedUpdate)
        {
            ReceivedUpdate = false;
            RealPosition = pos; // Need to test if this needs to be inverted
            GlobalRotation = rot;
            state.LinearVelocity = vel;
            AngularVelocity = angVel;
        }
        LinearVelocity = LinearVelocity.Clamp(MaxSpeed);
        base._IntegrateForces(state);
    }
}