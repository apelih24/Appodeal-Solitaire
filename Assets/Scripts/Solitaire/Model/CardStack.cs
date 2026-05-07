using System;
using System.Collections.Generic;

namespace Appodeal.Solitaire
{
    public class CardStack
    {
        private readonly List<CardData> _cards = new();

        public string Id { get; }
        public IReadOnlyList<CardData> Cards => _cards;
        public int Count => _cards.Count;
        public bool IsEmpty => _cards.Count == 0;

        public CardStack(string id)
        {
            Id = id;
        }

        public void Push(CardData card)
        {
            _cards.Add(card);
        }

        public CardData Pop()
        {
            if (IsEmpty)
                throw new InvalidOperationException($"Cannot pop from empty stack '{Id}'.");

            var card = _cards[^1];
            _cards.RemoveAt(_cards.Count - 1);
            return card;
        }

        public CardData Peek()
        {
            if (IsEmpty)
                throw new InvalidOperationException($"Cannot peek empty stack '{Id}'.");

            return _cards[^1];
        }

        public void Clear()
        {
            _cards.Clear();
        }
    }
}
