# Forest Friends Quest — Live Operations Guide

## Philosophy

Live operations in Forest Friends Quest follow a **warm-not-manipulative** ethic:
- Events create wonder, not FOMO.
- Seasonal content enhances the world without gating core gameplay.
- Returning players are welcomed warmly, not punished for absence.

---

## Seasonal Calendar (Year 1)

| Month | Season | Live Event | Bundle |
|-------|--------|-----------|--------|
| Jan | Winter | Frost Festival | Winter Theme |
| Feb | Winter | Valentine Forest Hearts | Starter Pack |
| Mar | Spring | Blossom Awakening | Spring Theme |
| Apr | Spring | Raindrop Ritual Weekend | — |
| May | Spring | First Creature Anniversary | — |
| Jun | Summer | Midsummer Firefly Dance | Summer Theme |
| Jul | Summer | Skyroot Canopy Opening | Premium Decor |
| Aug | Summer | Ancient Observatory Night | Ancient Lore Pack |
| Sep | Autumn | Harvest Moon Gathering | Autumn Theme |
| Oct | Autumn | Glowshroom Lantern Festival | Druid Lore Pack |
| Nov | Autumn | Gratitude Circle Ritual | Creature Album |
| Dec | Winter | Winter Solstice Sanctuary | All-Access Bundle |

---

## Event Architecture

### SeasonalEventSystem
Reads event definitions from `LiveContentPipeline`. Each event has:
- `startDate` / `endDate` (UTC)
- `eventId` (matched against `ForestSaveData.attendedSeasonalEventIds`)
- `rewards` — cosmetic items (no gameplay advantages)
- `ritualId` — triggers a special daily ritual during the event window

### Event Content Delivery
Events are either:
1. **Baked** — always in the build, gated by date check in `SeasonalEventSystem`
2. **Remote** — loaded via `AddressableContentManager.HotSwapSeasonalBundle()` at event start

For hot content injection without a full update:
```csharp
// In LiveContentPipeline:
AddressableContent.HotSwapSeasonalBundle("winter_frost", onComplete: ready =>
{
    if (ready) SeasonalEvents.StartEvent("frost_festival");
});
```

---

## Remote Configuration

Use Firebase Remote Config to control:

| Key | Type | Default | Purpose |
|---|---|---|---|
| `daily_ritual_count` | int | 3 | Rituals offered per day |
| `session_soft_cap_minutes` | int | 60 | Retention system warning threshold |
| `event_enabled_frost_festival` | bool | false | Hot-enable seasonal events |
| `difficulty_scale_factor` | float | 1.0 | Global difficulty adjustment |
| `iap_starter_price_override` | string | "" | Price display string override |
| `new_content_banner_text` | string | "" | New content announcement text |

```csharp
// Fetch remote config on session start:
FirebaseRemoteConfig.DefaultInstance
    .FetchAsync(TimeSpan.FromHours(12))
    .ContinueWith(_ => FirebaseRemoteConfig.DefaultInstance.ActivateAsync());
```

---

## Content Update Pipeline

### Minor Update (no app store submission)
1. Author new content JSON in `LiveContentPipeline`
2. Rebuild Addressables bundles: `Window → Asset Management → Addressables → Build`
3. Upload new bundles + catalog to CDN
4. Update `ContentVersionManager` version strings
5. Old clients fetch new catalog automatically on next launch

### Major Update (app store submission)
1. Increment `ForestSaveData.version` if schema changed
2. Add migration rule to `ContentVersionManager.MigrateSave()`
3. Build new AAB/IPA
4. Submit through standard review pipeline

---

## Push Notification Schedule

### Daily reminder (optional, parent-enabled)

| Time | Message |
|------|---------|
| 4:00 PM | "Pip is waiting at the Whispering Meadow! 🌿" |
| 6:00 PM | "The fireflies are gathering for evening ritual! ✨" |

### Event notifications
- 24h before event start: "The Frost Festival arrives tomorrow!"
- On event start: "The Frost Festival has begun in your sanctuary!"
- 48h before event end: "Last days of the Blossom Awakening!"

Notifications are handled by Unity Notifications (local) or Firebase
Cloud Messaging (remote). Both are gated behind explicit parent opt-in
in the parent dashboard.

---

## KPI Dashboard

### Daily KPIs to monitor
- DAU (Daily Active Users)
- Session length average
- Puzzle completion rate
- Daily ritual participation rate
- D1 / D7 / D30 retention cohorts

### Health alerts
Set Firebase BigQuery alerts for:
- Puzzle failure rate > 40% on any type (difficulty spike)
- Session length < 3 min average (engagement drop)
- Onboarding funnel completion < 60% (UX friction)
- IAP conversion rate < 1% after 7 days (store upsell issues)

---

## Patch Response Timeline

| Severity | Response | Deploy |
|---|---|---|
| P0 — crash on launch | Fix + emergency build same day | 24h app store expedite |
| P1 — save corruption | Fix + hotfix CDN config | 48h |
| P2 — progression blocker | Fix in next scheduled update | 1 week |
| P3 — cosmetic bug | Log + batch in next sprint | 2 weeks |

---

## Community Management

### Discord Structure
```
#announcements       — event launches, patch notes
#parent-zone         — parent dashboard tips, family advice
#creature-gallery    — share Memory Scrapbook screenshots
#bug-reports         — #P3 triage by community manager
#dev-updates         — behind-the-scenes development diary
```

### Weekly Dev Diary Topics
- Week 1: Creature character spotlight
- Week 2: Behind-the-scenes puzzle design
- Week 3: Seasonal event teaser
- Week 4: Community fan art feature
