# Solitaire Undo System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a simplified Solitaire prototype with drag-and-drop card movement between 3 stacks and a full undo system using Command pattern.

**Architecture:** Command Pattern + MVC. Pure C# model layer (CardData, CardStack, GameState), Command system (ICommand, MoveCardCommand, CommandsHistory) with ID-based storage for serializability, MonoBehaviour views (CardView, CardDragHandler, StackView), and manager orchestration (GameManager, UIManager). Standalone Tweening module for animations.

**Tech Stack:** Unity 6 (6000.3.6f1), URP 2D, C#, TextMeshPro, New Input System EventSystem interfaces.

---

## File Map

| File | Responsibility |
|------|---------------|
| `Assets/Scripts/Tweening/Tweening.asmdef` | Assembly definition for Tweening module (standalone, no refs) |
| `Assets/Scripts/Tweening/TweenService.cs` | Coroutine-based position tweening with ease-out quad |
| `Assets/Scripts/Solitaire/Solitaire.asmdef` | Assembly definition for Solitaire (refs: Tweening, TMP, UI) |
| `Assets/Scripts/Solitaire/Model/CardData.cs` | Suit/Rank enums + CardData class (pure C#) |
| `Assets/Scripts/Solitaire/Model/CardStack.cs` | List-backed stack with Push/Pop/Peek + indexed access |
| `Assets/Scripts/Solitaire/Model/GameState.cs` | Collection of CardStacks with lookup methods |
| `Assets/Scripts/Solitaire/Commands/ICommand.cs` | Command interface: Execute/Undo |
| `Assets/Scripts/Solitaire/Commands/MoveCardCommand.cs` | ID-based move command with Execute/Undo |
| `Assets/Scripts/Solitaire/Commands/CommandsHistory.cs` | Command stack with execute/undo/clear |
| `Assets/Scripts/Solitaire/Views/CardView.cs` | Card visual rendering (SpriteRenderer + TMP) |
| `Assets/Scripts/Solitaire/Views/StackView.cs` | Stack position calculator + drop zone identity |
| `Assets/Scripts/Solitaire/Views/CardDragHandler.cs` | Drag-and-drop input via EventSystem interfaces |
| `Assets/Scripts/Solitaire/Managers/GameManager.cs` | Central orchestrator: model ↔ view ↔ commands |
| `Assets/Scripts/Solitaire/Managers/UIManager.cs` | UI button bindings (Undo, New Game) |

---

### Task 1: Tweening Module

**Files:**
- Create: `Assets/Scripts/Tweening/Tweening.asmdef`
- Create: `Assets/Scripts/Tweening/TweenService.cs`

- [ ] **Step 1: Create Tweening.asmdef**

```json
{
    "name": "Tweening",
    "rootNamespace": "Appodeal.Tweening",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create TweenService.cs**

```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Appodeal.Tweening
{
    public class TweenService : MonoBehaviour
    {
        private readonly Dictionary<Transform, Coroutine> _activeTweens = new();

        public void MoveTo(Transform target, Vector3 destination, float duration, Action onComplete = null)
        {
            if (target == null) return;

            CancelTween(target);

            var coroutine = StartCoroutine(MoveCoroutine(target, destination, duration, onComplete));
            _activeTweens[target] = coroutine;
        }

        public void CancelTween(Transform target)
        {
            if (_activeTweens.TryGetValue(target, out var existing))
            {
                StopCoroutine(existing);
                _activeTweens.Remove(target);
            }
        }

        public void CancelAll()
        {
            StopAllCoroutines();
            _activeTweens.Clear();
        }

        private IEnumerator MoveCoroutine(Transform target, Vector3 destination, float duration, Action onComplete)
        {
            var start = target.position;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                target.position = Vector3.Lerp(start, destination, EaseOutQuad(t));
                yield return null;
            }

            target.position = destination;
            _activeTweens.Remove(target);
            onComplete?.Invoke();
        }

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    }
}
```

- [ ] **Step 3: Verify compilation**

Open Unity, confirm Tweening assembly appears in project and compiles without errors.

---

### Task 2: Data Model

**Files:**
- Create: `Assets/Scripts/Solitaire/Model/CardData.cs`
- Create: `Assets/Scripts/Solitaire/Model/CardStack.cs`
- Create: `Assets/Scripts/Solitaire/Model/GameState.cs`

- [ ] **Step 1: Create CardData.cs with Suit and Rank enums**

```csharp
namespace Appodeal.Solitaire
{
    public enum Suit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades
    }

    public enum Rank
    {
        Ace = 1,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King
    }

    public class CardData
    {
        public Suit Suit { get; }
        public Rank Rank { get; }
        public bool IsFaceUp { get; set; }

        public CardData(Suit suit, Rank rank, bool isFaceUp = false)
        {
            Suit = suit;
            Rank = rank;
            IsFaceUp = isFaceUp;
        }

        public bool IsRed => Suit is Suit.Hearts or Suit.Diamonds;

        public string DisplayRank => Rank switch
        {
            Rank.Ace => "A",
            Rank.Jack => "J",
            Rank.Queen => "Q",
            Rank.King => "K",
            _ => ((int)Rank).ToString()
        };

        public string DisplaySuit => Suit switch
        {
            Suit.Hearts => "♥",
            Suit.Diamonds => "♦",
            Suit.Clubs => "♣",
            Suit.Spades => "♠",
            _ => "?"
        };
    }
}
```

- [ ] **Step 2: Create CardStack.cs**

```csharp
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
```

- [ ] **Step 3: Create GameState.cs**

```csharp
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
```

---

### Task 3: Command System

**Files:**
- Create: `Assets/Scripts/Solitaire/Commands/ICommand.cs`
- Create: `Assets/Scripts/Solitaire/Commands/MoveCardCommand.cs`
- Create: `Assets/Scripts/Solitaire/Commands/CommandsHistory.cs`

- [ ] **Step 1: Create ICommand.cs**

```csharp
namespace Appodeal.Solitaire
{
    public interface ICommand
    {
        void Execute(GameState state);
        void Undo(GameState state);
    }
}
```

- [ ] **Step 2: Create MoveCardCommand.cs**

```csharp
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
```

- [ ] **Step 3: Create CommandsHistory.cs**

```csharp
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
```

---

### Task 4: Solitaire Assembly Definition

**Files:**
- Create: `Assets/Scripts/Solitaire/Solitaire.asmdef`

- [ ] **Step 1: Create Solitaire.asmdef**

```json
{
    "name": "Solitaire",
    "rootNamespace": "Appodeal.Solitaire",
    "references": [
        "Tweening",
        "Unity.TextMeshPro",
        "UnityEngine.UI"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

> **Note:** If Unity 6 reports missing assembly references for `Unity.TextMeshPro` or `UnityEngine.UI`, verify the exact assembly names in the Unity editor via the asmdef inspector's reference picker. TMP may be merged into `Unity.ugui` in Unity 6.

- [ ] **Step 2: Verify compilation**

Open Unity, confirm both Solitaire and Tweening assemblies compile. Check no namespace or reference errors in Console.

---

### Task 5: CardView

**Files:**
- Create: `Assets/Scripts/Solitaire/Views/CardView.cs`

- [ ] **Step 1: Create CardView.cs**

```csharp
using TMPro;
using UnityEngine;

namespace Appodeal.Solitaire
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CardView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _rankText;
        [SerializeField] private TextMeshPro _suitText;
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private Color _redColor = new(0.8f, 0.1f, 0.1f);
        [SerializeField] private Color _blackColor = new(0.1f, 0.1f, 0.1f);
        [SerializeField] private Color _faceDownColor = new(0.2f, 0.3f, 0.8f);

        public CardData Data { get; private set; }

        public void Initialize(CardData data)
        {
            Data = data;
            Refresh();
        }

        public void Refresh()
        {
            if (Data == null) return;

            if (Data.IsFaceUp)
            {
                _rankText.text = Data.DisplayRank;
                _suitText.text = Data.DisplaySuit;
                var color = Data.IsRed ? _redColor : _blackColor;
                _rankText.color = color;
                _suitText.color = color;
                _background.color = Color.white;
            }
            else
            {
                _rankText.text = string.Empty;
                _suitText.text = string.Empty;
                _background.color = _faceDownColor;
            }
        }

        public void SetSortingOrder(int order)
        {
            _background.sortingOrder = order;
            _rankText.sortingOrder = order + 1;
            _suitText.sortingOrder = order + 1;
        }

        public int GetSortingOrder() => _background.sortingOrder;
    }
}
```

---

### Task 6: StackView

**Files:**
- Create: `Assets/Scripts/Solitaire/Views/StackView.cs`

- [ ] **Step 1: Create StackView.cs**

```csharp
using UnityEngine;

namespace Appodeal.Solitaire
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class StackView : MonoBehaviour
    {
        [SerializeField] private string _stackId;
        [SerializeField] private float _cardOffsetY = -0.3f;

        public string StackId => _stackId;

        public Vector3 GetCardPosition(int index)
        {
            return transform.position + new Vector3(0f, index * _cardOffsetY, 0f);
        }
    }
}
```

---

### Task 7: CardDragHandler

**Files:**
- Create: `Assets/Scripts/Solitaire/Views/CardDragHandler.cs`

- [ ] **Step 1: Create CardDragHandler.cs**

```csharp
using Appodeal.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Appodeal.Solitaire
{
    [RequireComponent(typeof(CardView))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class CardDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private CardView _cardView;
        private GameManager _gameManager;
        private TweenService _tweenService;
        private Camera _mainCamera;
        private Vector3 _originalPosition;
        private int _originalSortingOrder;

        public bool IsDragging { get; private set; }

        private const int DragSortingOrder = 1000;
        private const float SnapBackDuration = 0.2f;

        public void Initialize(GameManager gameManager, TweenService tweenService)
        {
            _gameManager = gameManager;
            _tweenService = tweenService;
            _cardView = GetComponent<CardView>();
            _mainCamera = Camera.main;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_cardView.Data == null || !_cardView.Data.IsFaceUp) return;
            if (!_gameManager.IsTopCard(_cardView.Data)) return;

            IsDragging = true;
            _originalPosition = transform.position;
            _originalSortingOrder = _cardView.GetSortingOrder();
            _cardView.SetSortingOrder(DragSortingOrder);
            _tweenService.CancelTween(transform);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;

            var worldPos = _mainCamera.ScreenToWorldPoint(eventData.position);
            worldPos.z = transform.position.z;
            transform.position = worldPos;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!IsDragging) return;
            IsDragging = false;

            var targetStack = FindStackUnderPointer(eventData.position);
            if (targetStack != null)
            {
                var sourceStackId = _gameManager.FindStackContaining(_cardView.Data);
                if (sourceStackId != null && sourceStackId != targetStack.StackId)
                {
                    _gameManager.ExecuteMove(sourceStackId, targetStack.StackId,
                        _cardView.Data.Suit, _cardView.Data.Rank);
                    return;
                }
            }

            _cardView.SetSortingOrder(_originalSortingOrder);
            _tweenService.MoveTo(transform, _originalPosition, SnapBackDuration);
        }

        private StackView FindStackUnderPointer(Vector2 screenPosition)
        {
            var worldPos = _mainCamera.ScreenToWorldPoint(screenPosition);
            var hits = Physics2D.OverlapPointAll(worldPos);
            foreach (var hit in hits)
            {
                var stackView = hit.GetComponent<StackView>();
                if (stackView != null) return stackView;
            }
            return null;
        }
    }
}
```

---

### Task 8: GameManager

**Files:**
- Create: `Assets/Scripts/Solitaire/Managers/GameManager.cs`

- [ ] **Step 1: Create GameManager.cs**

```csharp
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
```

---

### Task 9: UIManager

**Files:**
- Create: `Assets/Scripts/Solitaire/Managers/UIManager.cs`

- [ ] **Step 1: Create UIManager.cs**

```csharp
using UnityEngine;
using UnityEngine.UI;

namespace Appodeal.Solitaire
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _newGameButton;

        private void Start()
        {
            _undoButton.onClick.AddListener(OnUndoClicked);
            _newGameButton.onClick.AddListener(OnNewGameClicked);
        }

        private void Update()
        {
            _undoButton.interactable = _gameManager.IsUndoAvailable;
        }

        private void OnUndoClicked()
        {
            _gameManager.Undo();
        }

        private void OnNewGameClicked()
        {
            _gameManager.InitializeGame();
        }

        private void OnDestroy()
        {
            _undoButton.onClick.RemoveListener(OnUndoClicked);
            _newGameButton.onClick.RemoveListener(OnNewGameClicked);
        }
    }
}
```

---

### Task 10: Editor Wiring & Verification

This task is for the human to complete in the Unity Editor. No scripts to write.

- [ ] **Step 1: Scene setup**

1. Main Camera: add `Physics2DRaycaster` component
2. Create EventSystem GameObject with `InputSystemUIInputModule` (not legacy StandaloneInputModule)

- [ ] **Step 2: Create Card prefab**

1. Create empty GameObject "Card"
2. Add `SpriteRenderer` (white square sprite, size ~1.0 x 1.4)
3. Add child "RankText" with `TextMeshPro` component (font size ~4, alignment center)
4. Add child "SuitText" with `TextMeshPro` component (font size ~3, below rank)
5. Add `BoxCollider2D` (set as Trigger, match sprite size)
6. Add `CardView` component — wire _rankText, _suitText, _background references
7. Add `CardDragHandler` component
8. Save as prefab in `Assets/Prefabs/Card.prefab`

- [ ] **Step 3: Create Stack areas**

1. Create 3 empty GameObjects: "Stack_A", "Stack_B", "Stack_C"
2. Position them horizontally (e.g., x = -3, 0, 3)
3. Add `SpriteRenderer` to each (semi-transparent rect, visual placeholder for empty stack)
4. Add `BoxCollider2D` to each (set as Trigger, tall enough to cover cascaded cards)
5. Add `StackView` component — set `_stackId` to "A", "B", "C" respectively

- [ ] **Step 4: Create managers**

1. Create "GameManager" GameObject, add `GameManager` component
   - Wire: _stackViews (all 3 StackViews), _cardPrefab (Card prefab), _tweenService
   - Set: _cardsPerStack = 5
2. Create "TweenService" GameObject, add `TweenService` component
3. Create UI Canvas with "Undo" and "New Game" buttons
4. Create "UIManager" GameObject, add `UIManager` component — wire _gameManager, _undoButton, _newGameButton

- [ ] **Step 5: Play test**

1. Enter Play mode — 15 cards should appear across 3 stacks (5 each)
2. Drag top card from one stack to another — card should animate to new position
3. Click Undo — card should animate back
4. Try dragging non-top card — should not respond
5. Drop card in invalid area — should snap back
6. Click New Game — all cards should reset

---

### Task 11: README

**Files:**
- Create: `README.md`

- [ ] **Step 1: Create README.md**

```markdown
# Solitaire — Undo Move System

## What I Built

A simplified Solitaire prototype demonstrating an **undo system** using the **Command pattern** with clean MVC architecture.

**Features:**
- 3 card stacks with drag-and-drop card movement
- Full undo history (unlimited depth) via Command pattern
- Smooth tween animations for card movement and undo
- New Game functionality with deck shuffle and re-deal
- Only top cards are draggable; invalid drops snap back

**Architecture highlights:**
- **Model layer** (pure C#): `CardData`, `CardStack`, `GameState` — no MonoBehaviour dependencies, testable and serializable
- **Command system**: `ICommand` interface with `MoveCardCommand` storing stack IDs and card identity (not object references) — inherently serializable for future save/load
- **View layer**: `CardView` (visuals), `CardDragHandler` (input), `StackView` (layout) — each with single responsibility
- **Tweening module**: Standalone reusable coroutine-based animation service in its own assembly
- **Assembly definitions**: `Appodeal.Solitaire` and `Appodeal.Tweening` with proper dependency graph

## What I'd Improve With More Time

- **Solitaire rules**: Descending rank, alternating color validation for moves
- **Redo support**: Second command stack (trivial extension of existing CommandsHistory)
- **Multi-card drag**: Move card sequences between stacks
- **Card flip animation**: Visual flip when revealing face-down cards
- **Save/load**: Serialize CommandsHistory to JSON (commands already store IDs, not references)
- **Object pooling**: Reuse card GameObjects instead of Instantiate/Destroy on New Game
- **Visual polish**: Card shadows, stack labels, move counter, particle effects
- **Unit tests**: NUnit tests for model and command layers (pure C#, no Unity dependencies needed)

## AI-Assisted Development

This project was built with **Claude Code (Claude Opus)** as a pair programming partner.

**How AI was used:**
- **Architecture design**: Brainstormed Command pattern vs Memento vs ScriptableObject approaches. Chose Command + MVC based on extensibility and interview signal.
- **Code generation**: AI generated initial implementations of all scripts based on the approved design spec. Each file was reviewed and adjusted.
- **Design decisions**: AI recommended `List<T>` over `Stack<T>` for CardStack (need index access for rendering), ID-based command storage for serializability, and feature-based folder structure with assembly definitions.

**Prompting approach:**
- Started with the case study requirements and iteratively refined scope through Q&A
- Used structured brainstorming to explore approaches before writing any code
- Design spec was written and approved before implementation began
```

---

## Editor Setup Checklist (Quick Reference)

| Requirement | Where |
|------------|-------|
| `Physics2DRaycaster` | Main Camera |
| `EventSystem` + `InputSystemUIInputModule` | Scene root |
| All card/stack `BoxCollider2D` | Set as **Trigger** |
| Card prefab | `SpriteRenderer` + 2x `TextMeshPro` children + `CardView` + `CardDragHandler` + `BoxCollider2D` |
| Stack GameObjects | `StackView` + `BoxCollider2D` + unique `_stackId` |
| GameManager | Wire: _stackViews[], _cardPrefab, _tweenService |
| UIManager | Wire: _gameManager, _undoButton, _newGameButton |
