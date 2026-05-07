using System.Collections.Generic;

namespace Appodeal.Solitaire
{
    public class GameState
    {
        private readonly Dictionary<string, CardStack> _stacks = new();

        public IReadOnlyDictionary<string, CardStack> Stacks => _stacks;

        public CardStack GetStack(string id) => _stacks[id];

        public CardStack AddStack(string id)
        {
            var stack = new CardStack(id);
            _stacks[id] = stack;
            return stack;
        }

        public CardData FindCard(Suit suit, Rank rank)
        {
            foreach (var stack in _stacks.Values)
                foreach (var card in stack.Cards)
                    if (card.Suit == suit && card.Rank == rank)
                        return card;

            return null;
        }

        public string FindStackContaining(Suit suit, Rank rank)
        {
            foreach (var stack in _stacks.Values)
                foreach (var card in stack.Cards)
                    if (card.Suit == suit && card.Rank == rank)
                        return stack.Id;

            return null;
        }

        public void Clear()
        {
            foreach (var stack in _stacks.Values)
                stack.Clear();
        }
    }
}
