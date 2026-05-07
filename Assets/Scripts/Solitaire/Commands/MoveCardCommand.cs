namespace Appodeal.Solitaire
{
    public class MoveCardCommand : ICommand
    {
        public string SourceStackId { get; }
        public string TargetStackId { get; }
        public Suit CardSuit { get; }
        public Rank CardRank { get; }
        public bool WasFaceUp { get; }

        public MoveCardCommand(string sourceStackId, string targetStackId,
            Suit cardSuit, Rank cardRank, bool wasFaceUp)
        {
            SourceStackId = sourceStackId;
            TargetStackId = targetStackId;
            CardSuit = cardSuit;
            CardRank = cardRank;
            WasFaceUp = wasFaceUp;
        }

        public void Execute(GameState state)
        {
            var source = state.GetStack(SourceStackId);
            var target = state.GetStack(TargetStackId);
            var card = source.Pop();
            target.Push(card);
        }

        public void Undo(GameState state)
        {
            var source = state.GetStack(SourceStackId);
            var target = state.GetStack(TargetStackId);
            var card = target.Pop();
            card.IsFaceUp = WasFaceUp;
            source.Push(card);
        }
    }
}
