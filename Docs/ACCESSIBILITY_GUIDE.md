# Forest Friends Quest — Accessibility Guide

## Design Principle

Every child deserves to experience the magic of Forest Friends Quest.
Accessibility is not an afterthought — it is woven into every system.

---

## Accessibility System Map

| System | File | Purpose |
|--------|------|---------|
| `AccessibilityManager` | Systems/AccessibilityManager.cs | Central coordinator for all A11Y modes |
| `ReducedMotionController` | Systems/ReducedMotionController.cs | Stops all particle animation |
| `NintendoFeelSystem` | Polish/NintendoFeelSystem.cs | Respects reduced motion in every animation |
| `UIAnimationSystem` | UI/UIAnimationSystem.cs | Motion-safe squash/stretch |
| `AdaptiveVisualDensityScaler` | UI/AdaptiveVisualDensityScaler.cs | Reduces VFX for cognitive load |
| `CameraFeelController` | UI/CameraFeelController.cs | Disables camera shake in calm mode |
| `AccessibilitySettingsPanel` | Controllers/AccessibilitySettingsPanel.cs | Settings UI panel |
| `LocalizationManager` | Config/LocalizationManager.cs | Drives font scaling + RTL |

---

## Supported Accessibility Modes

### 1. Calm Mode
**What it does:**
- Reduces or removes all ambient animations
- Slows UI transitions to 50% speed
- Removes particle bursts from achievement unlocks
- Enables a softer, lower-contrast colour palette
- Disables camera shake entirely

**How to activate:** Parent dashboard → Accessibility → Calm Mode ON
**Persisted in:** `PlayerPrefs["FFQ.Access.CalmMode"]`

### 2. Reduced Motion
**What it does:**
- Stops all particle emission
- Replaces animated transitions with instant cuts
- Disables `FloatBob` and `SwayMotion` components
- Disables `FireflyTrailSystem` and `WaterRippleSystem`

**How to activate:** Parent dashboard → Accessibility → Reduced Motion ON  
**Also respects:** iOS/Android system reduced-motion setting (via `AccessibilityManager`)
**Persisted in:** `PlayerPrefs["FFQ.Access.ReducedMotion"]`

### 3. Colorblind Modes

| Mode | Algorithm | Affected Systems |
|------|-----------|-----------------|
| `Deuteranomaly` | Green-weak simulation | All UI colour palettes |
| `Protanomaly` | Red-weak simulation | Puzzle highlight colours |
| `Tritanomaly` | Blue-weak simulation | Night palette, water effects |
| `Monochromacy` | Greyscale conversion | Entire screen post-process |

Implemented via `AccessibilityManager.OnColorblindModeChanged`. Each
biome and puzzle palette has pre-authored colorblind-safe variants.

**Reference palettes:** [Paul Tol's colour schemes](https://personal.sron.nl/~pault/)

### 4. Large Text Mode
Font scale factor: 1.0x (default) → 1.4x (large) → 1.8x (extra-large)

All text uses `LocalizationManager.FontScale` multiplied onto `fontSize`.
All layout containers use `ContentSizeFitter` so text never clips.

### 5. High Contrast Mode
Increases minimum contrast ratio to 7:1 (WCAG AAA).
- Dialogue boxes: pure white text on near-black backgrounds
- Buttons: solid fills, no gradients
- Icons: outlined variants with 3px stroke

### 6. Touch Target Size
All interactive elements have a minimum touch target of **44×44 dp**
(Apple HIG / Google Material guidelines).

In small-text mode where buttons shrink, an invisible `EventTrigger`
rect expands the hit area to 44dp minimum.

---

## Puzzle Accessibility

### Visual Puzzles
- All colour-coded puzzle elements have a **shape + icon** secondary indicator
- Colorblind palette applied automatically when colorblind mode is active
- `LightReflectionPuzzle`: direction arrows added alongside colour

### Audio Puzzles
- `MusicPatternPuzzle`: visual note timeline displayed alongside audio
- All audio cues have corresponding visual feedback (waveform highlight)
- Hearing-impaired mode: visual metronome beats replace audio timing cues

### Cognitive Load
- `AdaptiveVisualDensityScaler` reduces on-screen elements for younger children
- Hint system requires only 2 consecutive failures to offer help
- Puzzles can be skipped via parent dashboard after 5 failures

---

## Screen Reader Compatibility

All UI elements have `accessibilityLabel` set on interactive components.
For Unity UI (UGUI), use the `AccessibilityNode` component (custom,
in `AccessibilityManager`) which wraps elements with:
- `AccessibilityRole.Button` / `.Text` / `.Image`
- `AccessibilityLabel` from `LocalizationManager`
- `AccessibilityHint` for contextual help

iOS: VoiceOver
Android: TalkBack

---

## Testing Checklist

- [ ] Calm Mode: all animations stop, no particles, camera static
- [ ] Reduced Motion: no particles, no transition animations
- [ ] Deuteranomaly: no green/red confusion in any puzzle
- [ ] Large Text 1.8x: no text truncation in any panel
- [ ] High Contrast: all elements pass 7:1 contrast ratio
- [ ] Touch targets: verify 44dp on all buttons at all font sizes
- [ ] VoiceOver/TalkBack: full onboarding completable without vision
- [ ] Controller: full game completable without touchscreen (Steam/Switch)

---

## Settings Persistence

All accessibility settings are stored immediately in `PlayerPrefs`:

| Setting | Key | Values |
|---------|-----|--------|
| Calm Mode | `FFQ.Access.CalmMode` | 0/1 |
| Reduced Motion | `FFQ.Access.ReducedMotion` | 0/1 |
| Colorblind Mode | `FFQ.Access.ColorblindMode` | none/deut/prot/trit/mono |
| Font Scale | `FFQ.Access.FontScale` | 1.0 / 1.4 / 1.8 |
| High Contrast | `FFQ.Access.HighContrast` | 0/1 |

Settings apply immediately without requiring an app restart.
