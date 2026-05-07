# Solitaire — Undo Move System

## Getting Started

### Requirements
- **Unity 6** (6000.3.6f1)

### How to Run
1. Clone the repository
2. Open the project
3. Open the scene: `Assets/Scenes/Solitaire.unity`
4. Press **Play**

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
- **Factory pattern**: Extract card and deck creation logic into a dedicated factory to decouple instantiation from game logic
- **Dependency injection**: Decouple scripts with a DI container (Zenject or VContainer) for better testability and modularity

## AI-Assisted Development

This project was built with **Claude Code (Claude Opus)** as a pair programming partner.

**How AI was used:**
- **Architecture design**: Brainstormed Command pattern vs Memento vs ScriptableObject approaches. Chose Command + MVC based on extensibility and interview signal.
- **Code generation**: AI generated initial implementations of all scripts based on the approved design spec. Each file was reviewed and adjusted.
- **Design decisions**: AI recommended `List<T>` over `Stack<T>` for CardStack (need index access for rendering and multi-card moves), ID-based command storage for serializability, and feature-based folder structure with assembly definitions.

**Prompting approach:**
- Started with the case study requirements and iteratively refined scope through Q&A
- Used structured brainstorming to explore approaches before writing any code
- Design spec was written and approved before implementation began
