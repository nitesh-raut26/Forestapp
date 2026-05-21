# Forest Friends Quest — QA Checklist

## Severity Levels
- **P0** — Prevents launch (crash, save corruption, payment failure)
- **P1** — Blocks major feature (progression impossible, data loss)
- **P2** — Degrades experience (bug is visible, workaround exists)
- **P3** — Minor polish issue (cosmetic, non-blocking)

---

## 1. Core Gameplay QA

### 1.1 Onboarding
- [ ] App launches cold (first install) without crash
- [ ] Bootstrap sequence completes in < 3 seconds (Tier 1 device)
- [ ] Tutorial completes fully without skipping any step
- [ ] `FirstBondSequence` fires correctly after creature selection
- [ ] Onboarding dialogue is readable at default font size
- [ ] Back navigation does not break tutorial state
- [ ] Explorer tier selection (Sprout/Scout/Druid) persists correctly

### 1.2 Puzzle System (11 puzzle types)
- [ ] RotatingPathPuzzle — all rotation states valid, no stuck states
- [ ] PressureGatePuzzle — gate opens precisely on correct weight
- [ ] LightReflectionPuzzle — reflection angle maths correct
- [ ] MusicPatternPuzzle — sequence length scales with difficulty
- [ ] LogicMirrorPuzzle — mirror state never flickers
- [ ] RuneSequencePuzzle — sequence randomised each attempt
- [ ] MemoryTrailPuzzle — trail length correct per difficulty
- [ ] TimeMemoryChallenge — timer pauses correctly on pause
- [ ] All puzzles: hint triggers after 2 consecutive failures
- [ ] All puzzles: skip option available after 5 failures (parent-unlocked)
- [ ] Puzzle completion triggers correct VFX, SFX, dialogue sequence
- [ ] Puzzle SFX does not persist after puzzle exits

### 1.3 Creature Interaction
- [ ] All 6 creatures load with correct sprites
- [ ] Bond levels persist correctly after app restart
- [ ] All 6 creature greetings vary by time of day
- [ ] Feed treat interaction: loved-it vs neutral response fires correctly
- [ ] Evolution trigger fires at correct bond threshold
- [ ] Evolution cinematic plays and completes without hang
- [ ] Post-evolution bond state persists

### 1.4 Sanctuary
- [ ] Drag and drop works correctly on all tested screen sizes
- [ ] Grid placement snaps to correct cells
- [ ] No two decorations can overlap
- [ ] Remove decoration returns item to inventory
- [ ] Sanctuary state persists after app restart
- [ ] Campfire ritual fires at correct time-of-day
- [ ] Seasonal visual theme changes on season tick
- [ ] Creature homes correctly reflect owner bond level

---

## 2. Save System QA

### 2.1 Normal Save/Load
- [ ] Save writes to `forestquest_save.json` on pause
- [ ] Save writes to `forestquest_save.json` on quit
- [ ] Load restores all bond levels correctly
- [ ] Load restores sanctuary state correctly
- [ ] Load restores achievement flags correctly
- [ ] Load restores region unlock state correctly

### 2.2 Corruption Recovery
- [ ] Delete `forestquest_save.json` → backup is used
- [ ] Delete both files → fresh save created, no crash
- [ ] Corrupt JSON in primary file → fallback to backup gracefully
- [ ] Corrupt JSON in both files → fresh save, log warning, no crash

### 2.3 Migration
- [ ] Version 1 save loads correctly in version 4
- [ ] Version 3 save loads correctly in version 4
- [ ] `explorerTier` defaults to "scout" on old saves
- [ ] `sanctuaryGridItems` migrated from `placedItems` correctly

### 2.4 Edge Cases
- [ ] Save during app kill (home button mid-play) — data not lost
- [ ] 100 memory scrapbook cards — save/load all 100 correctly
- [ ] Maximum sanctuary items placed — save handles array size

---

## 3. Mobile Memory & Performance QA

### 3.1 Tier 1 Device (budget Android, 3GB RAM)
- [ ] App launches without OOM crash
- [ ] Sustained 30 fps through 30-minute session
- [ ] Memory stays below 256 MB (verify with Android Studio Profiler)
- [ ] No ANR (App Not Responding) in any tested flow
- [ ] Zone transition < 1 second
- [ ] `PerformanceManager` correctly activates Tier 1 mode
- [ ] VFX disabled on Tier 1 without visual breaks

### 3.2 Tier 2 Device (mid-range)
- [ ] Sustained 45 fps through 30-minute session
- [ ] Ambient VFX visible at reduced density
- [ ] Memory below 384 MB

### 3.3 Tier 3 Device (iPad Pro / flagship Android)
- [ ] Sustained 60 fps throughout
- [ ] Full VFX pipeline active
- [ ] Memory below 512 MB

### 3.4 Thermal Testing
- [ ] Device does not heat to uncomfortable level in 60-minute session
- [ ] `PerformanceManager` reduces particles when thermal pressure detected

---

## 4. Accessibility QA

- [ ] Calm Mode: all particle systems pause, camera static, slower transitions
- [ ] Reduced Motion: no particles, no transition animations, creature animation freezes
- [ ] Deuteranomaly: green/red puzzle elements distinguishable
- [ ] Protanomaly: red elements have shape or icon fallback
- [ ] Tritanomaly: blue elements have pattern fallback
- [ ] Large Text 1.8x: no text truncation in any panel (test all 10 languages)
- [ ] High Contrast: all text passes 7:1 contrast (use browser contrast checker)
- [ ] Touch targets: all buttons ≥ 44dp on iPhone SE 2 screen size
- [ ] VoiceOver (iOS): full onboarding completable with screen off
- [ ] TalkBack (Android): creature names read correctly

---

## 5. Localisation QA

Test all critical flows in each language:

| Flow | EN | ES | FR | DE | JA | KO | PT | IT | NL | ZH |
|------|----|----|----|----|----|----|----|----|----|----|
| Main menu | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Tutorial | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Dialogue | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Puzzle text | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Achievements | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Store / IAP | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |
| Parent dashboard | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ |

CJK font atlas loaded:
- [ ] All Japanese characters render without placeholder box
- [ ] All Korean characters render without placeholder box
- [ ] Simplified Chinese renders without placeholder box

---

## 6. Audio QA

- [ ] Music transitions cross-fade correctly between biomes (no pop)
- [ ] Night music transitions at correct time-of-day tick
- [ ] Puzzle SFX plays on completion
- [ ] Puzzle SFX does NOT play if sound is disabled in settings
- [ ] Campfire crackling loops seamlessly
- [ ] Creature voice lines play at correct trigger points
- [ ] No audio orphan (sound plays after scene state clears)
- [ ] Audio resumes correctly after phone call interruption (iOS)
- [ ] Headphone detection: spatial audio behaviour on AirPods correct

---

## 7. Analytics Verification

- [ ] `session_start` event fires on every launch (verify in DebugBuild log)
- [ ] `puzzle_completed` fires on every puzzle success
- [ ] `creature_bond` fires on every bond level-up
- [ ] `premium_conversion` fires on IAP purchase
- [ ] No event contains any PII (scan parameter values in logs)
- [ ] `analytics_enabled` defaults to false (verify in fresh install)
- [ ] Parent opt-in from dashboard enables tracking
- [ ] Firebase Dashboard shows events within 24h of release build test

---

## 8. IAP Verification

- [ ] All 10 products load from store correctly
- [ ] Parental PIN gate appears before any purchase
- [ ] Math gate appears if no PIN is set
- [ ] Cooldown prevents brute-force in 30-second window
- [ ] Purchase success: cosmetic unlocked immediately
- [ ] Purchase success: `premiumUnlocked` saved to disk
- [ ] Restore purchases: previously purchased items restored on new device
- [ ] Offline: owned items still unlocked without internet connection
- [ ] Sandbox testing: Apple/Google sandbox environment tested

---

## 9. Offline Mode Testing

- [ ] App works fully with airplane mode after first launch
- [ ] Addressables fall back to Resources/ correctly without internet
- [ ] IAP owned items available offline (cached entitlements)
- [ ] Analytics queue flushes when connection returns
- [ ] Save file writes do not require internet

---

## 10. Addressables Failure Testing

- [ ] Biome bundle load fails gracefully (no crash, use fallback background)
- [ ] Seasonal bundle unavailable: seasonal event does not start (no crash)
- [ ] Core bundle load fails: warning logged, game uses built-in fallbacks
- [ ] CDN unreachable: game proceeds with local Resources/ fallback

---

## 11. Tablet Scaling QA

Test on:
- iPad 9.7" (768×1024 portrait)
- iPad Pro 12.9" (1024×1366 portrait)
- Samsung Galaxy Tab A 10.1" (800×1280)
- Kindle Fire HD 10 (800×1280)

Check:
- [ ] Canvas scales correctly on all resolutions
- [ ] No UI elements clipped at edges
- [ ] Puzzle areas fill screen appropriately
- [ ] Dialogue boxes don't overlap puzzle areas
- [ ] Touch targets are not too large on tablets (scale down gracefully)

---

## Pre-Ship Sign-Off

| Area | Tester | Date | Status |
|------|--------|------|--------|
| Core gameplay | | | ☐ |
| Save system | | | ☐ |
| Performance (Tier 1) | | | ☐ |
| Accessibility | | | ☐ |
| Localisation (EN/ES/JA) | | | ☐ |
| IAP (sandbox) | | | ☐ |
| Analytics | | | ☐ |
| Offline mode | | | ☐ |
| Parent dashboard | | | ☐ |
