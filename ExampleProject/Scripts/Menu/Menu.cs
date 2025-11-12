using Godot;

public partial class Menu : Control
{
    [Export] private Menu PreviousMenu;
    public void Enter(){
        Visible = true;
    }
    public void Leave(){
        Visible = false;
    }
    public Menu GoToPreviousMenu()
    {
        Leave();
        PreviousMenu.Enter();
        return PreviousMenu;
    }
}