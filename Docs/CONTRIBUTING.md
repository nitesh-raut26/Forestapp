# Contributing to Forest Friends Quest

## Getting Started

1. Install **Unity 2022.3 LTS** via Unity Hub
2. Clone the repository
3. Open the project in Unity
4. Press ▶ Play — no scene setup needed

The `ForestQuestBootstrap` static initializer creates the entire game at runtime.

---

## Branch Strategy

```
main           — production-ready code only
develop        — integration branch
feature/[name] — new systems or content
fix/[name]     — bug fixes
release/[x.y]  — release preparation branch
hotfix/[name]  — emergency production fixes
```

PRs into `main` require:
- 1 code review approval
- CI passing (build + static analysis)
- Changelog entry

---

## Code Standards

### Namespace

All code lives in `namespace ForestFriendsQuest`. No exceptions.

### MonoBehaviour Initialization

Systems must not use `Awake()` for cross-system dependency resolution.
Use the `Initialize(...)` pattern:

```csharp
// Correct — called in dependency order by ForestSystemsContainer
public void Initialize(SaveSystem save, EmotionalBondingEngine bonding)
{
    _save    = save;
    _bonding = bonding;
}

// Incorrect — Awake() runs before ForestSystemsContainer can inject deps
private void Awake() { _save = FindObjectOfType<SaveSystem>(); }
```

### Events

- Use `event Action<T>` (never `UnityEvent<T>` for code-side wiring)
- Always unsubscribe in `OnDestroy()`
- Name events: `OnNounVerbed` (e.g. `OnBossDefeated`, `OnRegionUnlocked`)

### No Direct System Lookups

```csharp
// Correct
var bonding = ForestSystemsContainer.Instance.BondingEngine;

// Incorrect
var bonding = FindObjectOfType<EmotionalBondingEngine>();
```

### Object Pooling

```csharp
// Correct
var card = _pool.Get<CreatureCard>();
// ... use card ...
_pool.Return(card);

// Incorrect
var card = Instantiate(cardPrefab);
// ... use card ...
Destroy(card.gameObject);
```

### Comments

Write comments only when the WHY is non-obvious. Don't explain what
the code does — well-named identifiers already do that.

```csharp
// Correct — explains a non-obvious constraint
// Force unload deferred by 2s to avoid mid-frame GC spike.
yield return new WaitForSeconds(2f);
yield return Resources.UnloadUnusedAssets();

// Incorrect — restates what the code already says
// This increments the bond level by one.
state.bondLevel++;
```

---

## Adding a New System

1. Create `Assets/Scripts/[Category]/[SystemName].cs`
2. Add the system as a `public [Type] SystemName { get; private set; }` property in
   `ForestSystemsContainer.cs`
3. Add initialization in `InitializeAll()` in the correct dependency order
4. If the system needs to be accessible game-wide, use
   `ForestSystemsContainer.Instance.SystemName`
5. Wire event subscriptions in `ForestSystemsContainer.InitializeAll()` — not inside
   the system itself
6. Update `Docs/SYSTEM_ARCHITECTURE.md` with the new system entry

---

## Adding a New Puzzle Type

1. Create `Assets/Scripts/Puzzle/[TypeName]Puzzle.cs`
2. Implement `IForestPuzzle` interface
3. Register in `PuzzleManager.RegisterPuzzleTypes()`
4. Add a `PuzzleType` enum value
5. Wire `PuzzleSFXManager` callbacks for this type
6. Add QA test cases to `Docs/QA_CHECKLIST.md`

---

## Adding a New Analytics Event

1. Add named method to `FirebaseAnalyticsConnector.cs`
2. Call from `AnalyticsEventRouter.cs`
3. Document in `Docs/ANALYTICS_GUIDE.md`

---

## Pull Request Checklist

- [ ] Code compiles without warnings
- [ ] New system follows `Initialize()` pattern
- [ ] No `FindObjectOfType` in hot paths
- [ ] No `Instantiate`/`Destroy` for pooled objects
- [ ] Events unsubscribed in `OnDestroy()`
- [ ] No PII in any analytics events
- [ ] Accessibility: new UI elements have minimum 44dp touch targets
- [ ] Performance: no new `Update()` without justification
- [ ] Documentation updated if system is new

---

## Style Guide Quick Reference

| Item | Convention |
|------|-----------|
| Private fields | `_camelCase` |
| Public properties | `PascalCase` |
| Events | `OnNounVerbed` |
| Constants | `ALL_CAPS` |
| Interfaces | `IForestSomething` |
| Region dividers | `// ─── Title ───` |
| File encoding | UTF-8 BOM-less |
| Line endings | LF (Unix) |
| Indent | 4 spaces (no tabs) |
