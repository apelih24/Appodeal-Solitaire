namespace Appodeal.Solitaire
{
    public interface ICommand
    {
        void Execute(GameState state);
        void Undo(GameState state);
    }
}
