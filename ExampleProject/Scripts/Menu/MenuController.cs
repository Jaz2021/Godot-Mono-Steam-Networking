using Godot;

public partial class MenuController : Control
{
    public static MenuController Instance
    {
        get;
        private set;
    }
    public override void _Ready()
    {
        Instance?.QueueFree();
        Instance = this;
        defaultSubMenu.Enter();
        currentMenu = defaultSubMenu;
    }
    [Export] private Menu defaultSubMenu;
    protected Menu currentMenu;
    public void EnterMenu()
    {
        defaultSubMenu.Enter();
    }
    public void EnterMenu(Menu newMenu){
        currentMenu.Leave();
        currentMenu = newMenu;
        newMenu.Enter();
    }
    public void LeaveMenu()
    {
        currentMenu.Leave();
    }
    public void GoToPreviousMenu()
    {
        currentMenu = currentMenu.GoToPreviousMenu();

    }
}