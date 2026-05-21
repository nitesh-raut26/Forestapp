# Forest Friends Quest — Save System Pipeline

## Design Goals

1. **Zero data loss** on unexpected app kill (background save on every pause)
2. **Corruption resilience** via dual-file + PlayerPrefs triple fallback
3. **Migration safety** via versioned `ForestSaveData` and `ContentVersionManager`
4. **Cloud-save compatible** via flat JSON structure (Firebase / iCloud / Google Drive)
5. **No PII stored** — all creature IDs and lore IDs are opaque string keys

---

## Storage Layers

```
Persistence Fallback Chain (load-time):

  1. persistentDataPath/forestquest_save.json    ← primary
         │ (corrupt / missing)
         ▼
  2. persistentDataPath/forestquest_backup.json  ← rotation backup
         │ (corrupt / missing)
         ▼
  3. PlayerPrefs["ForestFriendsQuest.Save"]       ← legacy migration
         │ (empty / missing)
         ▼
  4. new ForestSaveData()                         ← fresh game
```

On every successful save the primary file is written first, then the
backup is overwritten with the same content. This is intentional: the
backup is a same-session guard against mid-write crashes, not a rollback
to a previous session. Use cloud save for cross-session rollback.

---

## ForestSaveData Schema (version 4)

```csharp
ForestSaveData {
  int     version                   // schema version for migration
  bool    premiumUnlocked           // any IAP purchase applied
  bool    soundEnabled
  int     totalLevelAttempts
  int     totalHintsUsed
  int     totalWrongAnswers
  string  explorerTier              // "sprout" | "scout" | "druid"
  int     sproutGrowth
  int     forestTreats
  int     pipBond, mimiBond, ...    // 1 int per creature (6 creatures)
  int     elderwood, riverCrystals, fireflyDust, ancientSap  // crafting resources
  string[]  craftedItemIds
  string[]  attendedSeasonalEventIds
  string[]  discoveredLoreIds
  string[]  defeatedBossIds
  string[]  unlockedRegionIds
  string[]  creatureEvolutionState  // "creatureId:stageIndex" tuples
  string[]  unlockedAchievementIds
  int     totalInGameDays
  int     currentSeasonIndex
  PlacedSanctuaryItem[] placedItems
  PlacedItem[]          sanctuaryGridItems
  LevelProgressData[]   levelProgress
}
```

---

## Auto-Save Behaviour

| Trigger | Action |
|---------|--------|
| Every 120 seconds (Update loop) | `SaveSystem.Save()` |
| `OnApplicationPause(true)` | `SaveSystem.Save()` |
| `OnApplicationQuit()` | `SaveSystem.Save()` |
| Achievement unlock | `SaveSystem.SetAchievementUnlocked()` + immediate persist |
| IAP purchase | `SaveSystem.Save()` via `PremiumUnlockController` |

---

## Save Migration

`ContentVersionManager.MigrateSave(data)` is called once on startup
before any system reads the save. Migration rules:

| From Version | To Version | Change |
|---|---|---|
| 1 | 2 | Added `explorerTier` field; default = "scout" |
| 2 | 3 | Added `creatureEvolutionState`; default = empty |
| 3 | 4 | Added `sanctuaryGridItems` (new placement format); migrated from `placedItems` |

New fields added to `ForestSaveData` must have C# default values that
represent a safe "first-time" state. Never remove a field; instead,
deprecate it with `[Obsolete]` and keep it in the struct for one major
version before removal.

---

## Cloud Save Integration

The save file is a flat JSON blob — no binary data, no Unity-specific
types. To enable cloud save:

### Firebase Realtime Database
```csharp
var json = JsonUtility.ToJson(saveData);
dbRef.Child("saves").Child(anonymousUserId).SetValueAsync(json);
```

### Google Play Games / Game Center
Both platforms accept a `byte[]` payload. Convert:
```csharp
var bytes = System.Text.Encoding.UTF8.GetBytes(json);
// pass to ISavedGameClient.CommitUpdate()
```

### iCloud Key-Value Store
```csharp
NSUbiquitousKeyValueStore.DefaultStore["ffq_save"] = json;
NSUbiquitousKeyValueStore.DefaultStore.Synchronize();
```

---

## Debugging Save State

Open `DebugToolkit` in the editor overlay (requires `ReleaseConfiguration.IsDebug`):

- **"Dump Save"** — logs the current JSON to console.
- **"Reset Save"** — calls `SaveSystem.ResetAll()`. Irreversible.
- **"Force Migrate"** — runs `ContentVersionManager.MigrateSave()` on the loaded data.
- **"Set Bond (pip, 10)"** — directly sets bond levels for testing evolution triggers.

---

## COPPA Considerations

The save file is stored locally on-device only. If cloud save is enabled,
the studio must ensure:
- The cloud storage key is not linked to an email, name, or IDFA.
- The key is a randomly generated anonymous device ID (`SystemInfo.deviceUniqueIdentifier`
  should NOT be used — generate and persist a UUID instead).
- Parent consent is collected before any save data leaves the device.
