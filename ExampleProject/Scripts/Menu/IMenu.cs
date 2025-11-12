public interface IMenu
{
    public void Enter(IMenu previous);
    public void Enter();
    public void Leave();
}