using Godot;

public partial class MainMenu : MenuController
{
    [Export] private Menu NewGameMenu;
    public void NewGamePressed()
    {
        GD.Print("Received new game pressed");
        EnterMenu(NewGameMenu);
    }
    public void StartOnlineGame()
    {
        GD.Print("Start Online game");
        Globals.Instance.StartOnlineGame(new(null, Vector3.Zero, Vector3.Zero), null);
    }
    public void StartOfflineGame()
    {
        GD.Print("Start offline game");
        Globals.Instance.StartOfflineGame();
    }
    public void QuitPressed()
    {
        GetTree().Quit();
    }
    public override void _Input(InputEvent e)
    {
        if(e.IsActionPressed("Pause"))
        {
            // GD.Print("Going to previous menu");
            GoToPreviousMenu();

        }
    }
}