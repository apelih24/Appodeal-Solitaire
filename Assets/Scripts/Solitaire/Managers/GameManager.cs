using System.Collections.Generic;
using Appodeal.Tweening;
using UnityEngine;

namespace Appodeal.Solitaire
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private StackView[] _stackViews;
        [SerializeField] private CardView _cardPrefab;
        [SerializeField] private TweenService _tweenService;
        [SerializeField] private int _cardsPerStack = 5;
        [SerializeField] private float _animationDuration = 0.2f;

        private GameState _gameState;
        private CommandsHistory _commandsHistory;
        private readonly Dictionary<CardData, CardView> _cardViews = new();

        public bool IsUndoAvailable => _commandsHistory != null && _commandsHistory.IsUndoAvailable;

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            CleanupCards();

            _gameState = new GameState();
            _commandsHistory = new CommandsHistory();

            foreach (var stackView in _stackViews)
                _gameState.AddStack(stackView.StackId);

            var deck = CreateDeck();
            Shuffle(deck);

            int totalCards = Mathf.Min(_cardsPerStack * _stackViews.Length, deck.Count);
            for (int i = 0; i < totalCards; i++)
            {
                var card = deck[i];
                card.IsFaceUp = true;
                var stackId = _stackViews[i % _stackViews.Length].StackId;
                _gameState.GetStack(stackId).Push(card);
            }

            SpawnCardViews();
        }

        public void ExecuteMove(string sourceStackId, string targetStackId, Suit suit, Rank rank)
        {
            var card = _gameState.FindCard(suit, rank);
            if (card == null) return;

            var command = new MoveCardCommand(sourceStackId, targetStackId, suit, rank, card.IsFaceUp);
            _commandsHistory.ExecuteCommand(command, _gameState);
            RefreshAllViews();
        }

        public void Undo()
        {
            if (!IsUndoAvailable) return;

            _commandsHistory.UndoLastCommand(_gameState);
            RefreshAllViews();
        }

        public string FindStackContaining(CardData card)
        {
            return _gameState.FindStackContaining(card.Suit, card.Rank);
        }

        public bool IsTopCard(CardData card)
        {
            var stackId = FindStackContaining(card);
            if (stackId == null) return false;

            var stack = _gameState.GetStack(stackId);
            return !stack.IsEmpty && stack.Peek() == card;
        }

        private void RefreshAllViews()
        {
            foreach (var stackView in _stackViews)
            {
                var stack = _gameState.GetStack(stackView.StackId);
                for (int i = 0; i < stack.Count; i++)
                {
                    var card = stack.Cards[i];
                    if (!_cardViews.TryGetValue(card, out var cardView)) continue;

                    var dragHandler = cardView.GetComponent<CardDragHandler>();
                    if (dragHandler != null && dragHandler.IsDragging) continue;

                    var targetPos = stackView.GetCardPosition(i);
                    cardView.SetSortingOrder(i * 2);
                    cardView.Refresh();
                    _tweenService.MoveTo(cardView.transform, targetPos, _animationDuration);
                }
            }
        }

        private void SpawnCardViews()
        {
            foreach (var stackView in _stackViews)
            {
                var stack = _gameState.GetStack(stackView.StackId);
                for (int i = 0; i < stack.Count; i++)
                {
                    var card = stack.Cards[i];
                    var position = stackView.GetCardPosition(i);
                    var cardObj = Instantiate(_cardPrefab, position, Quaternion.identity);

                    var cardView = cardObj.GetComponent<CardView>();
                    cardView.Initialize(card);
                    cardView.SetSortingOrder(i * 2);

                    var dragHandler = cardObj.GetComponent<CardDragHandler>();
                    dragHandler.Initialize(this, _tweenService);

                    _cardViews[card] = cardView;
                }
            }
        }

        private void CleanupCards()
        {
            foreach (var cardView in _cardViews.Values)
            {
                if (cardView != null)
                    Destroy(cardView.gameObject);
            }
            _cardViews.Clear();
        }

        private static List<CardData> CreateDeck()
        {
            var deck = new List<CardData>();
            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
                foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
                    deck.Add(new CardData(suit, rank));
            return deck;
        }

        private static void Shuffle(List<CardData> deck)
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
        }
    }
}
