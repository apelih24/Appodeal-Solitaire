using System.Collections.Generic;

namespace Appodeal.Solitaire
{
    public class CommandsHistory
    {
        private readonly Stack<ICommand> _history = new();

        public bool IsUndoAvailable => _history.Count > 0;

        public void ExecuteCommand(ICommand command, GameState state)
        {
            command.Execute(state);
            _history.Push(command);
        }

        public void UndoLastCommand(GameState state)
        {
            if (!IsUndoAvailable) return;

            var command = _history.Pop();
            command.Undo(state);
        }

        public void Clear()
        {
            _history.Clear();
        }
    }
}
