# Forest Friends Quest — Release Roadmap

## Version History Target

| Version | Target | Focus |
|---------|--------|-------|
| 0.8.0 | Alpha | Internal playtest — all systems functional |
| 0.9.0 | Beta | External playtest — art placeholder, core loop complete |
| 1.0.0 | Launch | Full art, voice, 3 biomes, tutorial complete |
| 1.1.0 | +4 weeks | 2 more biomes, Spring seasonal event |
| 1.2.0 | +8 weeks | Summer event, IAP launch, analytics live |
| 2.0.0 | +6 months | Creature album, parent premium features, 3 more biomes |

---

## Phase 1 — Beta Testing Plan (weeks 1-6)

### Recruitment
- 50 families with children ages 5–12
- Mix of devices: 20% budget Android, 50% mid-range, 30% iPad
- Geographic mix: US, UK, Germany, Japan (localisation testing)
- 5 families with children who have visual or motor accessibility needs

### Beta Goals
1. Onboarding completion rate ≥ 60%
2. D7 retention ≥ 20%
3. Average session ≥ 5 minutes
4. 0 P0 crashes in 1000+ sessions
5. Parent dashboard used by ≥ 30% of parents

### Beta Channels
- Apple TestFlight (iOS)
- Google Play Internal Testing (Android)
- Discord community server (invite-only, 50 families)

### Feedback Collection
- In-game: 3-question survey on session 3 (stars 1-5 + free text)
- Weekly 15-minute video call with 5 selected families
- Parent dashboard telemetry export with consent

---

## Phase 2 — Soft Launch (weeks 7-10)

### Target Markets
- New Zealand (iOS + Android) — English, small market, quick iteration
- Canada — French + English
- Australia — English, high tablet penetration

### Soft Launch KPIs
| KPI | Target |
|-----|--------|
| D1 Retention | ≥ 40% |
| D7 Retention | ≥ 22% |
| D30 Retention | ≥ 10% |
| Average Session | ≥ 7 min |
| Crash-free sessions | ≥ 99.5% |
| Store rating | ≥ 4.5 stars |

### Soft Launch Decision Gates
- If D1 < 35%: review onboarding, iterate
- If D7 < 18%: review day 2-6 content hooks
- If crash rate > 0.5%: P0 hotfix before global launch
- If rating < 4.3: review most-mentioned pain points

---

## Phase 3 — Global Launch

### Launch Day Checklist
- [ ] All P0/P1 QA items resolved
- [ ] App Store metadata final (all localisations)
- [ ] Google Play listing final (all localisations)
- [ ] Press kit available: screenshots, trailer, character art
- [ ] Discord community live and moderated
- [ ] Firebase Analytics live and monitoring
- [ ] Push notification system tested and parent opt-in flow verified
- [ ] CDN capacity tested for launch surge (10x expected traffic)
- [ ] On-call engineer scheduled for launch weekend
- [ ] Crash monitoring (Firebase Crashlytics) dashboard ready

### Launch Marketing
- 3-week countdown social campaign (TikTok, Instagram)
- Launch trailer: 60 seconds, creature bond moment featured
- Parent-focused launch: "The game made for family evenings"
- Educational press: Edutopia, Common Sense Media review submission
- Children's YouTuber gifted access (2 weeks pre-launch)
- App Store feature pitch: submit to Apple Kids category curator 4 weeks early

---

## App Store Optimisation

### Keywords (iOS / Google Play)
Primary: educational game children, kids puzzle game, creature adventure  
Secondary: calm game kids, forest adventure, learning game age 5, family game  
Long-tail: game like animal crossing for kids, cozy game children

### Screenshot Strategy (5 required, 10 recommended)
1. Creature meeting — emotional hook (Pip greeting player)
2. Sanctuary customisation — shows creative expression
3. World map — communicates scale
4. Puzzle gameplay — shows educational content
5. Seasonal event — shows ongoing content
6. Parent dashboard — targets adult purchaser
7. Evolution reveal moment — rewards visible
8. Memory scrapbook — shows emotional investment
9. Campfire ritual — shows daily engagement
10. Boss encounter — shows challenge progression

### App Store Description Structure
```
Line 1-2: Emotional hook — "A magical living world..."
Line 3-4: Core promise — "200 puzzles, 6 companions..."
Line 5-6: For parents — "Trusted by families..."
Line 7-8: CTA — "Download free..."
```

---

## Steam Launch Strategy

### Target: 3 months post-mobile launch

### Steam-Specific Features
- Achievements (Steam) wired to `AchievementSystem`
- Trading cards (6 creature cards + foil variants)
- Cloud save via Steamworks
- Steam Deck verified build (controller + touchscreen)
- Family Sharing enabled

### Steam Store Setup
- Launch in Early Access first (collect feedback, build community)
- Target 1.0 release 6 months post-Early-Access
- Launch bundle: Game + Soundtrack + Digital Art Book

---

## Retention KPI Targets (Year 1)

| Metric | 3 months | 6 months | 12 months |
|--------|----------|----------|-----------|
| DAU | 10,000 | 25,000 | 50,000 |
| D30 Retention | 12% | 15% | 18% |
| Avg session | 8 min | 10 min | 12 min |
| IAP conversion | 2% | 3% | 4% |
| Premium ARPU | $2.50 | $3.00 | $3.50 |
| Store rating | 4.5 | 4.6 | 4.7 |

---

## Patch Schedule

| Patch | Trigger | Target Timeline |
|-------|---------|----------------|
| Hotfix | P0 crash or save corruption | Same day |
| Emergency | P1 progression blocker | 48 hours |
| Monthly | P2/P3 accumulation + content | 4-week cadence |
| Major | New biome / season / creature | Quarterly |

---

## Trailer Script Brief

### Hero Trailer (60s — App Store, YouTube)
```
0:00-0:05  Wide establishing shot of the forest. Dawn light. Silence.
0:05-0:12  Pip appears, looks at camera, tilts head, beckons.
0:12-0:20  Montage: 4 puzzle types in 2-second cuts. Solve → celebration.
0:20-0:30  Creature bonding moment — morning greeting, feed treat, bond level-up glow.
0:30-0:40  Sanctuary: placing decorations, seasonal shift (spring → winter).
0:40-0:48  Boss encounter — dramatic VFX, creatures assemble.
0:48-0:55  Evolution reveal — slow, beautiful. All 6 creatures evolved, together.
0:55-1:00  Title card: "Forest Friends Quest". App Store badges. "Free to play."
```

### Parent-Focused Trailer (30s — social ads)
```
0:00-0:05  Real family, tablet, child laughing.
0:05-0:15  Game highlights: safe, colorful, educational.
0:15-0:25  Parent dashboard: "See what your child is learning."
0:25-0:30  "Download free today."
```
