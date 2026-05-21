# Forest Friends Quest — Performance Guide

## Device Tier Targets

| Tier | Examples | Target FPS | Memory Budget |
|------|---------|-----------|--------------|
| Tier 1 (Low) | Budget Android tablets, 3GB RAM | 30 fps | 256 MB |
| Tier 2 (Mid) | iPad Air, Galaxy Tab A, 4GB RAM | 45 fps | 384 MB |
| Tier 3 (High) | iPad Pro, flagship Android, 6GB+ | 60 fps | 512 MB |

`DeviceCapabilityProfiler` detects tier at startup using RAM + GPU scoring.
`PerformanceManager` applies the corresponding quality preset.

---

## Quality Preset Matrix

| Feature | Tier 1 | Tier 2 | Tier 3 |
|---------|--------|--------|--------|
| Particle density | 10% | 50% | 100% |
| Glow effects | Off | Reduced | Full |
| Firefly trail system | Off | 50 particles | 200 particles |
| Water ripple resolution | Off | 128px | 512px |
| Ambient VFX | Off | Reduced | Full |
| Shadow quality | Off | Low | Medium |
| Texture compression | ETC2 | ETC2 | ASTC |
| Audio sample rate | 22050 Hz | 44100 Hz | 44100 Hz |
| Target frame rate | 30 | 45 | 60 |
| Addressable streaming | Tight budget | Normal | Preload |

---

## Memory Budget Rules

`MemoryBudgetController` enforces these limits per tier:

```
Tier 1: 256 MB total
  ├─ Audio: 32 MB
  ├─ Textures: 128 MB
  ├─ VFX Pool: 16 MB
  └─ Other: 80 MB

Tier 2: 384 MB total
  ├─ Audio: 64 MB
  ├─ Textures: 192 MB
  ├─ VFX Pool: 48 MB
  └─ Other: 80 MB

Tier 3: 512 MB total
  ├─ Audio: 96 MB
  ├─ Textures: 256 MB
  ├─ VFX Pool: 96 MB
  └─ Other: 64 MB
```

When memory pressure rises above 80% of budget, `MemoryBudgetController`:
1. Unloads distant biome audio clips
2. Reduces ambient particle emission by 50%
3. Releases unused Addressable bundles (ref-count = 0)

---

## Object Pooling Rules

All runtime-spawned objects must use `ObjectPoolManager` or `UIPoolManager`.
**Never call `Instantiate()` or `Destroy()` for recurring game objects.**

Common pool categories:
- VFX particles: `EmotionalParticleEngine`, `FireflyTrailSystem`, `WaterRippleSystem`
- UI cards: `ReusableCardPool`
- Dialogue bubbles: pooled in `DynamicDialogueSystem`
- Sanctuary decorations: pooled in `SanctuaryDecorationSystem`

---

## Update() Budget

Target: **≤10 MonoBehaviour.Update() calls** on Tier 1.

| System | Update Strategy |
|--------|----------------|
| `DayNightWeatherController` | Update() — drives time tick |
| `SaveSystem` | Update() — auto-save timer only |
| `ReducedMotionController` | No Update() — event-driven |
| `NintendoFeelSystem` | Coroutines only |
| `EmotionalParticleEngine` | Particle System native update |
| `DynamicSeasonManager` | Coroutine polling (every 60s) |
| `PerformanceManager` | 1-second interval timer |
| `MemoryBudgetController` | 5-second interval timer |
| Creature AI | `InvokeRepeating` — not Update() |

**Rule:** any system that runs every frame must be justified. Prefer
`InvokeRepeating`, coroutines, or event-driven patterns.

---

## GC Allocation Prevention

| Pattern | Avoid | Use Instead |
|---------|-------|------------|
| LINQ in hot path | `.Where().Select()` | pre-allocated arrays |
| String concatenation | `"a" + b + "c"` | `System.Text.StringBuilder` |
| Anonymous lambdas | `() => DoThing()` in loops | cached `Action` fields |
| Dictionary boxing | `Dict<string, object>` in hot path | typed structs |
| `Resources.UnloadUnusedAssets()` | synchronous call | async coroutine (fixed in v3.1) |
| `FindObjectOfType<T>()` | in Update() | cached at Awake() |

---

## Addressables Streaming Budget

| Bundle | Strategy | Size Target |
|--------|---------|------------|
| `core_ui` | Load on boot, never unload | < 8 MB |
| `core_audio` | Load on boot, never unload | < 16 MB |
| `creatures` | Load on boot, never unload | < 12 MB |
| Biome bundles | Load on zone enter, unload on exit | < 10 MB each |
| Seasonal bundles | Load on event start, unload after | < 6 MB each |

---

## Frame Time Budget (Tier 1, 30 FPS = 33ms/frame)

| System | Budget |
|--------|--------|
| Unity rendering | 12 ms |
| UI layout | 3 ms |
| Physics (none in 2D game) | 0 ms |
| Animation | 4 ms |
| Game logic (all systems) | 8 ms |
| Audio | 3 ms |
| Margin | 3 ms |

---

## Profiling Checkpoints

Before each release build, profile with Unity Profiler on:
1. **Cold launch** — target < 3s to first interactive frame
2. **Zone transition** — target < 0.5s stutter on Tier 1
3. **Sanctuary with 20+ decorations** — target steady 30 fps Tier 1
4. **Boss encounter VFX peak** — no frame drops below 25 fps Tier 1
5. **Session 30 minutes in** — memory must remain stable (no leak)

---

## Mobile Crash Prevention

| Risk | Mitigation |
|------|----------|
| OOM on 3GB RAM device | Tier 1 memory cap + aggressive audio unload |
| ANR (app not responding) | Never block main thread; all I/O is async |
| Thermal throttle | `PerformanceManager` monitors CPU temp; reduces particles on heat |
| Battery drain | `Application.runInBackground = false`; vsync off |
| Startup crash | `ForestQuestBootstrap` guards against double-init |
