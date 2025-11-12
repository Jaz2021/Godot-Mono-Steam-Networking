using Godot;


public partial class CameraController : Node3D
{
    private Camera3D cam;
    public override void _Ready()
    {
        cam = GetChild<Camera3D>(0);
    }
    public void Enable()
    {
        cam.Visible = true;
    }
    public void Disable()
    {
        cam.Visible = false;
    }
    public void AddRotation(Vector2 rotation)
    {
        Rotation += new Vector3(0f, -rotation.X, 0f);
        cam.Rotation += new Vector3(-rotation.Y, 0f, 0f);
        cam.Rotation = new Vector3(Mathf.Clamp(cam.Rotation.X - rotation.Y, -1.42f, 1.42f), 0f, 0f);
    }
    public float GetLookDir()
    {
        return Rotation.Y;
    }
}
