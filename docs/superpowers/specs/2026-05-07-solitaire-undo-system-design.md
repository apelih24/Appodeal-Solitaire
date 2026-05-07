# Solitaire Undo System — Design Spec

## Overview

Simplified Solitaire prototype with drag-and-drop card movement between 2–3 free stacks and a full undo system. No Solitaire rules enforced — any card can go anywhere. Focus: clean architecture, Command pattern undo, smooth animations.

Unity 6 (6000.3.6f1), URP 2D, New Input System.

## Architecture

**Command Pattern + MVC separation.**

- **Model:** Pure C# classes (no MonoBehaviour). Card data, stack data, game state.
- **View:** MonoBehaviours for rendering, drag-and-drop, animation.
- **Commands:** `ICommand` with Execute/Undo. ID-based (no object references) — inherently serializable.
- **Services:** Standalone reusable modules (Tweening).
- **Managers:** Orchestration layer connecting model ↔ view ↔ commands.

## Data Model

### CardData (class, Appodeal.Solitaire)
- `Suit Suit` — enum: Hearts, Diamonds, Clubs, Spades
- `Rank Rank` — enum: Ace through King
- `bool IsFaceUp` — visibility state
- Identity: Suit + Rank pair (unique, natural ID)

### CardStack (class, Appodeal.Solitaire)
- `string Id` — unique identifier
- Internal `List<CardData>` storage (not `Stack<T>` — needs index access for rendering and multi-card moves)
- API: `Push(card)`, `Pop()`, `Peek()`, `Count`, `Cards` (read-only indexed access)

### GameState (class, Appodeal.Solitaire)
- Collection of `CardStack` instances
- Lookup by ID: `GetStack(string id)`
- Provides card resolution: find card by Suit+Rank across all stacks

## Command System

### ICommand (interface, Appodeal.Solitaire)
- `void Execute(GameState state)`
- `void Undo(GameState state)`

### MoveCardCommand (class, Appodeal.Solitaire)
Stores value-type identifiers, resolves references at execution time:
- `string SourceStackId`
- `string TargetStackId`
- `Suit CardSuit`
- `Rank CardRank`
- `bool WasFaceUp` — original face state for restoration

`Execute()`: resolve stacks via GameState, pop from source, push to target.
`Undo()`: reverse — pop from target, push to source, restore `WasFaceUp`.

### CommandsHistory (class, Appodeal.Solitaire)
- `Stack<ICommand>` for executed commands
- `ExecuteCommand(ICommand cmd, GameState state)` — execute + push
- `UndoLastCommand(GameState state)` — pop + undo
- `bool IsUndoAvailable` — history not empty
- `Clear()` — reset for new game

## View Layer

### CardView (MonoBehaviour, Appodeal.Solitaire)
- Displays card visuals: rank text, suit symbol, background color (red/black)
- Simple colored rectangle with TextMeshPro text
- Updates visual from `CardData` reference
- No input handling (delegated to CardDragHandler)

### CardDragHandler (MonoBehaviour, Appodeal.Solitaire)
Separate component on card GameObject. Single responsibility: input.
- Implements `IPointerDownHandler`, `IDragHandler`, `IPointerUpHandler`
- On drag start: detach visually from stack, raise sorting order
- On drag: follow pointer position
- On drop: find nearest `StackView` via raycast/overlap, request move through `GameManager`
- On invalid drop: tween back to original position via `TweenService`
- `bool IsDragging` state flag

### StackView (MonoBehaviour, Appodeal.Solitaire)
- References `CardStack` model by ID
- Manages card layout: vertical offset for stacked cards
- Drop zone detection (collider area)
- `RefreshView()` — repositions all child `CardView`s based on model state, with animation

## Tweening Service

### TweenService (MonoBehaviour, Appodeal.Tweening)
Standalone reusable module. MonoBehaviour — needs coroutine host. No external dependencies.
- `MoveTo(Transform target, Vector3 destination, float duration, Action onComplete = null)`
- Coroutine-based lerp with ease-out quad
- Cancels active tween on same transform if new one starts
- Used by: CardDragHandler (snap back), StackView (refresh layout), Undo (animate return)

## Managers

### GameManager (MonoBehaviour, Appodeal.Solitaire)
Central orchestrator:
- Holds `GameState` and `CommandsHistory`
- `InitializeGame()` — create deck (52 cards or subset), shuffle, deal across 3 stacks
- `ExecuteMove(string sourceStackId, string targetStackId, Suit suit, Rank rank)` — create MoveCardCommand, execute, refresh views
- `Undo()` — undo last command, refresh views
- References to all `StackView`s for refresh calls

### UIManager (MonoBehaviour, Appodeal.Solitaire)
- Undo button → `GameManager.Undo()`
- New Game button → `GameManager.InitializeGame()`
- Binds `IsUndoAvailable` to undo button interactability

## Game Flow

1. **Init:** GameManager creates deck, shuffles, deals cards across 3 stacks. Views instantiate card GameObjects.
2. **Play:** Player drags card → drops on stack → GameManager.ExecuteMove() → command executed → views refresh with tween animation.
3. **Invalid drop:** Card tweens back to original position.
4. **Undo:** Button press → GameManager.Undo() → command reverted → views animate card back to source stack.
5. **New Game:** Reset GameState, clear CommandsHistory, re-deal, refresh all views.

## File Structure

```
Assets/Scripts/
├── Solitaire/
│   ├── Solitaire.asmdef              (refs: Tweening)
│   ├── Model/
│   │   ├── CardData.cs
│   │   ├── CardStack.cs
│   │   └── GameState.cs
│   ├── Commands/
│   │   ├── ICommand.cs
│   │   ├── MoveCardCommand.cs
│   │   └── CommandsHistory.cs
│   ├── Views/
│   │   ├── CardView.cs
│   │   ├── CardDragHandler.cs
│   │   └── StackView.cs
│   └── Managers/
│       ├── GameManager.cs
│       └── UIManager.cs
└── Tweening/
    ├── Tweening.asmdef                (standalone, no refs)
    └── TweenService.cs
```

All Solitaire scripts: `namespace Appodeal.Solitaire`
Tweening: `namespace Appodeal.Tweening`

## Conventions

- All bool names prefixed with `Is` (e.g., `IsFaceUp`, `IsDragging`, `IsUndoAvailable`)
- Feature-based folder structure with asmdef per feature
- Commands store IDs not references — inherently serializable
- No external dependencies beyond Unity built-ins + TextMeshPro

## Future Extensions (not in scope)

- Solitaire rules (descending order, alternating colors)
- Redo support (second stack in CommandsHistory)
- Save/load (serialize CommandsHistory to JSON)
- Multi-card drag (move sequences)
- Card flip commands
- Score system
