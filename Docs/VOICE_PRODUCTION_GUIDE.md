# Forest Friends Quest — Voice Production Guide

## Philosophy

> "Each creature should sound like someone a child genuinely loves."

Voice in Forest Friends Quest is not narration — it is relationship.
Every line should feel like something your best friend would say.
Warm, real, never condescending.

---

## Technical Specifications

### Recording Setup
- **Microphone:** Condenser (AKG C414, Neumann TLM103, or equivalent)
- **Environment:** Treated vocal booth, RT60 < 200ms
- **Sample rate:** 48000 Hz, 24-bit WAV
- **Gain:** -12 dBFS peak, no clipping, -23 LUFS integrated
- **Distance:** 12–18 inches from capsule, slight off-axis to reduce plosives
- **Pop filter:** Required for all sessions

### Post-Processing Chain
```
Input (48k/24bit WAV)
    │
    ├─ Noise reduction (RX De-noise — gentle, 6-8 dB reduction only)
    ├─ De-click / De-crackle (RX)
    ├─ EQ: low cut 100Hz, slight presence boost 2-5kHz
    ├─ De-esser (if needed)
    ├─ Light compression: 3:1, -3 dB GR
    ├─ Normalise to -3 dBFS peak
    └─ Export: 44100 Hz / 16-bit WAV → OGG Vorbis quality 8

Unity Import Settings:
  Load Type: Compressed In Memory
  Compression: Vorbis
  Sample Rate Setting: Preserve Sample Rate
  Force To Mono: false (keep stereo for music; mono for VO)
```

### File Naming Convention
```
vo_[creature]_[context]_[emotion]_[variant].[ext]

Examples:
  vo_pip_greeting_morning_happy_01.ogg
  vo_mimi_puzzle_hint_curious_01.ogg
  vo_tomo_bedtime_calm_02.ogg
  vo_luma_discovery_excited_01.ogg
  vo_nori_ritual_gentle_01.ogg
  vo_sol_lore_mysterious_01.ogg
```

---

## Character Voice Profiles

### Pip the Fox
**Voice Type:** Young, bright, slightly higher pitch  
**Vocal Age Feel:** 9–11 year old energy (not baby voice)  
**Gender:** Any — voice defines the character, not gender  
**Pitch Range:** Mid-high (around C5 fundamental)  
**Pacing:** Quick, energetic, occasionally breathless with excitement  
**Warmth Level:** ★★★★★ (highest — most lines)

**Key Qualities:**
- Sounds like they just had a great idea and can't wait to share it
- Laughs easily and genuinely
- Never sounds tired or jaded
- Slight upward inflection at end of sentences (curious not questioning)
- Pauses for thought — not scripted staccato

**Emotional Range:**
| Emotion | Direction |
|---------|-----------|
| Happy | Bright, forward-placed, slight smile in the voice |
| Excited | Faster pace, pitch rises 15%, slight breathlessness |
| Sad | Softer, slightly slower, but never loses warmth |
| Curious | Head-tilt quality — slightly raised, wondering inflection |
| Sleepy | Slower, lower, breathy, trailing ends |
| Brave | Deeper resonance, measured pace, confident |

**Reaction Sounds:**
- Agree: "Mmhm!", "Oh yes!", "That's it!"
- Discover: "Ohhhh...", "Ooooo!", "Did you see that?!"
- Wrong answer: "Hmm... let's try again", "That was close!"
- Tired: "Yawn... just five more minutes..."

**Sample Script (greeting):**
> "Good morning, Explorer! The meadow path has been waiting for us — and I think I spotted something sparkling near the creek this morning. Want to go see?"

---

### Mimi the Bird
**Voice Type:** Musical, lilting, naturally melodic speech  
**Vocal Age Feel:** Same age as Pip but more refined, musical education background  
**Pitch Range:** High soprano range — musical cadences even in speech  
**Pacing:** Flows like a song — rhythmic, never monotone  
**Warmth Level:** ★★★★☆

**Key Qualities:**
- Speech naturally rises and falls like a melody
- Sings brief phrases spontaneously (hum 3-note motif when happy)
- "oooh" and "mmm" sounds frequently — musical approval
- British-adjacent light accent welcome but not required

**Reaction Sounds:**
- Delight: "(musical hum) That's lovely!", "Like a beautiful song!"
- Discovery: "(gasp) Oh! Do you hear that?", "A new melody!"
- Puzzle hint: "Try listening... there's a pattern..."

**Sample Script (puzzle hint):**
> "Hmm... (hums gently) Think of it like a melody, Explorer. Each piece fits where it belongs — not forced, just... placed. Listen for where it wants to go."

---

### Tomo the Turtle
**Voice Type:** Deep, slow, warm baritone — old-soul quality  
**Vocal Age Feel:** Ancient wise elder — the one who has seen everything and is still patient  
**Pitch Range:** Low baritone  
**Pacing:** Deliberate, unhurried — let silence breathe between thoughts  
**Warmth Level:** ★★★★★

**Key Qualities:**
- Never rushes — the world can wait
- Long pauses are intentional (leave room in script)
- Occasional low warm chuckle
- Deep breaths between long thoughts
- Japanese story-master energy — each word chosen carefully

**Reaction Sounds:**
- Approval: "(deep chuckle) Mmm...", "Wisely done."
- Wrong: "(gentle breath) The river does not apologise for its course..."
- Bedtime: "(very slow, deep) Let the forest... breathe with you..."

**Sample Script (bedtime story):**
> "In the time before paths were named... (pause) there was a tree at the heart of the forest... (pause) and she knew every creature's dream... (pause) Come... let me tell you how she learned to dream herself."

---

### Luma the Firefly
**Voice Type:** Tiny, sparkling, slightly mischievous — glittery quality  
**Vocal Age Feel:** 6–8 year old playfulness but with hidden depths  
**Pitch Range:** High, light, slightly breathy — the smallest of the creatures  
**Pacing:** Quick bursts, then sudden quiet moments of wonder  
**Warmth Level:** ★★★★☆

**Key Qualities:**
- High-pitched but not squeaky — light and airy
- Giggles easily
- Makes glittering sounds with tongue (trills)
- Wonder is their primary state — everything is amazing

**Reaction Sounds:**
- Delight: "(giggle) Eeeee!", "Oooh, sparkly!", "Glowy glowy glowy!"
- Discovery: "(tiny gasp) Did you see?! DID YOU SEE?!"
- Dark/scary: (whispers) "Stay close... I'll keep the light bright..."

**Sample Script (guiding through cave):**
> "(whispers) Shhh... see those crystals? (tiny delighted squeak) They only glow when they feel safe. Just like me. (pause, warmly) And... just like you."

---

### Nori the Deer
**Voice Type:** Gentle, lyrical, soft — like wind through leaves  
**Vocal Age Feel:** Mature but gentle — like a calm older sibling  
**Pitch Range:** Mid-range, smooth, no harsh consonants  
**Pacing:** Moderate — never rushed, never too slow  
**Warmth Level:** ★★★★★

**Key Qualities:**
- Voice has a calming effect — used in highest-stakes moments
- Very little vibrato — still and certain
- Speaks like someone who has never needed to shout
- "Forest protector" quality — steady and unafraid

**Reaction Sounds:**
- Approval: "(soft exhale) Yes...", "Just as it should be."
- Alert: "(gentle but firm) Wait... do you feel that?"
- Comfort: "(very soft) It's alright. We'll find another way."

**Sample Script (sanctuary ritual):**
> "Stand still for a moment, Explorer. (pause) The forest is listening. (pause) When you're ready... (soft) you can tell it what you're grateful for."

---

### Sol the Owl
**Voice Type:** Rich, resonant, slightly mysterious — velvet darkness  
**Vocal Age Feel:** Oldest and most ancient — time itself given voice  
**Pitch Range:** Low-mid, resonant — a voice that fills a cave  
**Pacing:** Measured — every pause has meaning  
**Warmth Level:** ★★★★☆ (warmth beneath mystery)

**Key Qualities:**
- Slight echo or reverb in dry recording is fine — space in the voice
- Speaks mostly at night or during lore moments
- Knows things before they happen — a hint of foreknowledge
- Deep and warm — not threatening

**Reaction Sounds:**
- Approving: "(deep soft) Mmm. The forest knows you now."
- Lore: "(breath) In an age before memory..."
- Night greeting: "(very quiet) The stars are listening... what will you ask them?"

**Sample Script (lore discovery):**
> "(pause, as if reading) These marks... (pause) are older than the path you walked to find them. They say... that the first explorer heard the forest crying... and did not run. (longer pause) That is why the forest trusts us still."

---

## Recording Session Plan

### Session Duration
- 2 hours maximum per creature per session
- 30-minute warm-up included
- 15-minute breaks every 45 minutes

### Session Order Per Character
1. Warm-up reads (not for use — just breathing)
2. High-energy lines (excited, happy) — done while fresh
3. Neutral/guidance lines (hints, tutorials)
4. Emotional depth lines (sad, bedtime, lore)
5. Reaction sound bank (10-15 variations each)
6. Pick-up lines from previous sessions

### Script Template Format
```
[CONTEXT]: Tutorial step 3, player found first creature
[EMOTION]: Excited / warm
[PACING]: Quick but not rushed
[LINE]: "There you are! I knew you'd find me. (pause) Ready for our first adventure together?"
[NOTES]: Slight upward bounce on "There you are" — genuine surprise-delight
[ALT]: "Oh! It's you! I've been waiting — come on, there's so much to show you!"
```

### Localization-Safe Recording
- Leave 20% silence before and after each line (for timing adjustment in other languages)
- Record 3 variants of every important line (A/B/C) — different readings, same content
- German and French are ~30% longer than English — leave room in UI
- Japanese and Korean may require entirely re-recorded scripts with different character names
