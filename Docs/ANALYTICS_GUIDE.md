# Forest Friends Quest — Analytics Guide

## COPPA / GDPR Compliance Summary

| Requirement | Implementation |
|---|---|
| No PII collected | All event parameters use opaque IDs, duration buckets, tier strings |
| Child-directed treatment | `FirebaseAnalytics.SetAnalyticsCollectionEnabled(false)` by default |
| Parent opt-in required | `FirebaseAnalyticsConnector.EnableTracking(true)` only from parent dashboard |
| No advertising ID | IDFA / GAID never read |
| Data retention | Firebase project configured to 60-day auto-delete |
| GDPR right to erasure | `FirebaseAnalytics.ResetAnalyticsData()` exposed in parent dashboard |

---

## System Map

```
Game Events
    │
    ▼
AnalyticsEventRouter          — subscribes to all game-system events
    │
    ▼
FirebaseAnalyticsConnector    — COPPA-safe Firebase wrapper
    │
    ├─▶ Firebase Analytics (if package present + parent opted-in)
    └─▶ Debug log (editor / builds without Firebase)

RetentionCohortTracker        — session-level D1/D7/D30 cohort tracking
FunnelAnalysisSystem          — onboarding, engagement, conversion funnels
```

---

## Event Catalogue

### Session Events

| Event Name | Parameters | Trigger |
|---|---|---|
| `session_start` | `explorer_tier`, `is_first_session` | App launch |
| `session_end` | `duration_bucket`, `puzzles_attempted` | App pause/quit |
| `session_cohort` | `day_number`, `session_number` | Every session start |
| `retention_cohort` | `cohort` (d1/d3/d7/d14/d30) | Milestone session days |

### Gameplay Events

| Event Name | Parameters | Trigger |
|---|---|---|
| `puzzle_completed` | `puzzle_id`, `puzzle_type`, `duration_bucket`, `hint_used` | Puzzle success |
| `boss_defeated` | `boss_id` | Boss encounter complete |
| `region_unlocked` | `region_id` | World region discovery |
| `lore_discovered` | `lore_id` | Lore page collected |
| `creature_bond` | `creature_id`, `bond_level` | Bond level-up |
| `creature_evolved` | `creature_id`, `stage_name` | Evolution milestone |
| `ritual_participated` | `ritual_id` | Daily ritual completed |
| `sanctuary_customized` | `action`, `item_id` | Decoration placed/removed |

### Onboarding Events

| Event Name | Parameters | Trigger |
|---|---|---|
| `onboarding_step` | `step_id`, `completed` | Each onboarding step |
| `funnel_step` | `funnel_id`, `step`, extras | All funnel checkpoints |

### Accessibility Events

| Event Name | Parameters | Trigger |
|---|---|---|
| `accessibility_enabled` | `feature` | Calm mode, colorblind, reduced motion |

### Monetisation Events

| Event Name | Parameters | Trigger |
|---|---|---|
| `premium_conversion` | `product_id` | IAP purchase completed |
| `funnel_step` (conversion funnel) | `funnel_id=conversion`, `step` | Store open → purchase |

### Difficulty Spike Events

| Event Name | Parameters | Trigger |
|---|---|---|
| `difficulty_spike` | `puzzle_type`, `consecutive_fails` | 3+ failures in a row |
| `funnel_step` (difficulty funnel) | `funnel_id=difficulty`, `step` | Hint, fail, retry, skip |

---

## Duration Buckets

All time-based parameters use buckets to prevent micro-fingerprinting:

| Bucket | Range |
|--------|-------|
| `under_30s` | 0–29 seconds |
| `30s_1m` | 30–59 seconds |
| `1m_3m` | 1–3 minutes |
| `3m_10m` | 3–10 minutes |
| `over_10m` | 10+ minutes |

---

## BigQuery Schema (Firebase Export)

Standard Firebase export creates tables in BigQuery named `events_YYYYMMDD`.
Key columns for our custom events:

```sql
SELECT
  event_name,
  event_timestamp,
  event_params,
  (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'session_id') AS session_id,
  (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'funnel_id') AS funnel_id,
  (SELECT value.string_value FROM UNNEST(event_params) WHERE key = 'step') AS step
FROM `project.dataset.events_*`
WHERE event_name = 'funnel_step'
  AND _TABLE_SUFFIX BETWEEN '20250101' AND '20251231'
```

---

## Key Retention Metrics

### Target KPIs (year 1)

| Metric | Target | Notes |
|--------|--------|-------|
| D1 Retention | ≥ 45% | Industry avg for educational games: 35% |
| D7 Retention | ≥ 25% | |
| D30 Retention | ≥ 12% | |
| Onboarding completion | ≥ 70% | |
| Average session length | ≥ 8 min | |
| Daily ritual participation | ≥ 40% of DAU | |
| IAP conversion (7 days) | ≥ 2% | Cosmetics-only; lower than typical games |

### Difficulty Health Signals

If `puzzle_type=music_pattern` shows `consecutive_fails ≥ 3` for > 20% of sessions,
flag for difficulty review. Use `DynamicDifficultySystem` to ease automatically,
but manual review of puzzle design should follow.

---

## Dashboard Setup (Looker Studio)

Connect Firebase BigQuery export to Looker Studio and build:
1. **Retention Cohort Chart** — D1/D7/D30 by week of install
2. **Onboarding Funnel** — step completion rates
3. **Puzzle Difficulty Heat Map** — fail rate by puzzle type
4. **Accessibility Usage** — % of sessions with each mode enabled
5. **IAP Conversion Funnel** — store_open → gate_shown → purchased

---

## Adding New Analytics Events

1. Add a named method to `FirebaseAnalyticsConnector.cs`
2. Call it from `AnalyticsEventRouter.cs` (subscribe to the relevant game event)
3. If it's part of a user journey: add a `FunnelAnalysisSystem` step
4. Document the event in this guide
5. Verify in editor — enable `Debug.isDebugBuild` logging to confirm firing
