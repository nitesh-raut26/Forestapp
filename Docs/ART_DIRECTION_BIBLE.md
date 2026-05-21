# Forest Friends Quest — Art Direction Bible

## Visual Identity

### Core Feeling
> "A storybook that breathes."

Every frame should feel like a hand-illustrated children's book that has
come alive. Soft edges. Warm light. Colors that feel like dappled sunlight
through leaves — not flat digital palettes.

### Tone References
| Tone Pillar | Reference |
|---|---|
| Warmth | Studio Ghibli (My Neighbour Totoro, The Secret World of Arrietty) |
| Wonder | Monument Valley (architectural elegance, impossible spaces) |
| Coziness | Cozy Grove (seasonal depth, tactile textures) |
| Emotion | Spiritfarer (expressive silhouettes, emotional clarity) |
| Joy | Animal Crossing (bouncy, tactile, always happy to see you) |

---

## Master Color Palette

### Base Palette
```
Forest Deep:    #1A4D2E   (base shadow, midnight forest)
Forest Mid:     #2D7A4F   (foliage mid-tone)
Forest Bright:  #52B788   (highlight, young leaves)
Warm Amber:     #E8A838   (firefly, warm light, campfire)
Twilight Blue:  #2C4A6E   (night sky, moonlit water)
Blossom Pink:   #F4A6C0   (spring flowers, mimi's wings)
Stone Gray:     #9AAEA0   (ruins, ancient stone)
Cream White:    #F8F4E8   (parchment, dialogue boxes)
```

### Seasonal Palette Shifts
| Season | Primary Shift | Secondary Shift | Atmosphere |
|--------|-------------|----------------|-----------|
| Spring | +15% saturation on greens | Pink blossoms | Soft morning haze |
| Summer | +20% brightness | Deep cyan sky | Warm golden hour |
| Autumn | Orange/amber overlay | Copper foliage | Misty warm glow |
| Winter | -30% saturation | Blue-white highlights | Crisp cold clarity |

---

## Character Style Guide

### Overall Character Rules
- **Line art:** 3px stroke on outer edge, 1.5px inner detail
- **Shape language:** round and soft for friendly creatures, angular for challenges
- **Eyes:** large, expressive, always have a small highlight catchlight
- **Proportions:** stylised — larger heads (1/3 body), smaller limbs
- **Animation style:** squash/stretch — 15-20% on impact frames
- **Shadow:** single soft cast shadow, no hard-light shadows

---

## Character Profiles

### Pip the Fox
**Role:** Primary guide — curious, brave, always first to explore  
**Age feeling:** 8-year-old energy in a fox body  
**Colors:** Burnt sienna body `#C85A20`, cream belly `#F5E6C8`, amber eyes `#E8A838`  
**Shape language:** Triangle ears, bushy tail — energy and curiosity  
**Silhouette:** Instantly recognisable by tail + ear pair

**Animation States:**
| State | Key Frame Description |
|---|---|
| Idle | Gentle sway, tail flicks every 3s, eyes blink at 4-second intervals |
| Happy | Full body bounce, tail sweeps wide arc, ears perk forward |
| Sleepy | Head droops, eyes half-closed, tail wraps around body |
| Excited | Rapid ear flicks, tiny hops, sparkling eye highlights |
| Sad | Ears flatten, tail low, slow breathing cycle |
| Evolution Stage 1 | Base form — small, rounded |
| Evolution Stage 2 | More defined markings, slightly taller |
| Evolution Stage 3 | Golden trim on ears and tail, warm glow outline |

**Blink Animation:** 3-frame squash (frames: open, half-close, closed, half-close, open)
**Interaction Poses:** Reaching forward, turning to look at player, offering paw

---

### Mimi the Bird
**Role:** Musical guide — joyful, melodic, always singing  
**Colors:** Soft sky blue `#7EC8E3`, white breast `#F8F4E8`, rose-pink wing tips `#F4A6C0`  
**Shape language:** Round puffed body — warmth and gentleness  
**Silhouette:** Puffed chest, small crest, wide rounded wings

**Animation States:**
| State | Key Frame Description |
|---|---|
| Idle | Wing flutter every 2s, head tilts left-right |
| Happy | Full wing spread, rising slightly, musical notes particle |
| Sleepy | Tucked wings, head nestled, gentle chest swell |
| Excited | Rapid wing flutter, rising 20px, beak opens singing |
| Sad | Wings drooped, head down, small raindrop particle |

---

### Tomo the Turtle
**Role:** Wise elder — patient, storyteller, deep thinker  
**Colors:** Deep forest green shell `#2D7A4F` with amber rings `#E8A838`, sandy skin `#D4A873`  
**Shape language:** Perfect circles — stability and patience  
**Silhouette:** Dome shell, short wide legs

**Animation States:**
| State | Key Frame Description |
|---|---|
| Idle | Slow breathing, shell gently rises/falls every 4s |
| Happy | Head extends fully, slow content smile |
| Sleepy | Head retracted halfway, eyes closed |
| Excited | Head fully extended, front legs wave slowly |
| Sad | Fully retracted into shell, single sparkle |

---

### Luma the Firefly
**Role:** Explorer and light-bringer — mischievous, illuminates secrets  
**Colors:** Deep purple body `#4A2D6E`, golden bioluminescent glow `#FFE066`  
**Shape language:** Elongated teardrop, soft glow aura always present  
**Silhouette:** Tiny body surrounded by distinct soft glow

**Animation States:**
| State | Key Frame Description |
|---|---|
| Idle | Figure-8 flight path, glow pulses every 2s |
| Happy | Rapid figure-8s, glow brightens 3x |
| Sleepy | Slow drift, glow dims to 20% |
| Excited | Spiral upward, glow leaves trailing particle |
| Sad | Glow flickers, downward drift |

**Special:** Luma is the only character who emits actual scene light.
Use a Unity Light2D point light attached to Luma's GameObject.
Intensity: 0.4 base, 0.8 happy, 0.2 sleepy.

---

### Nori the Deer
**Role:** Guardian of the forest — graceful, calm, protective  
**Colors:** Warm tan `#C8A86E`, white spots `#F8F4E8`, deep brown antler nubs `#7A4A2D`  
**Shape language:** Long elegant curves — grace and stillness  
**Silhouette:** Tall slender neck, small antler nubs, long legs

**Animation States:**
| State | Key Frame Description |
|---|---|
| Idle | Gentle head turn, ear flicks, tail flick every 3s |
| Happy | Steps forward, head raises, soft snort particle |
| Sleepy | Eyes drift closed, head slowly lowers |
| Excited | Small gentle pronk (hop), ears fully forward |
| Sad | Turns away, looks over shoulder, single crystal tear |

---

### Sol the Owl
**Role:** Night guide — mysterious, ancient, keeper of lore  
**Colors:** Midnight blue feathers `#1A3050`, amber-gold eyes `#E8A838`, silver chest `#C8D4D8`  
**Shape language:** Perfect circle face disc — wisdom and watchfulness  
**Silhouette:** Circular face, swept back ear tufts, wide perch stance

**Animation States:**
| State | Key Frame Description |
|---|---|
| Idle | Head swivels slowly, feathers settle, blinks at 6s intervals |
| Happy | Fluffs feathers, slow wing spread, star particles |
| Sleepy | Eyes close, head tucks, feathers fluff |
| Excited | Rapid head swivel, rapid blink, ear tufts rise |
| Sad | Feathers flatten, head tilts down, single tear |

---

## Biome Art Guides

### 1. Whispering Meadow
**Palette:** `#52B788` greens, `#F8E8C0` warm golds, `#87CEEB` soft sky  
**Atmosphere:** Warm, safe, welcoming — first place players ever see  
**Lighting:** Midday warm sun, no hard shadows  
**VFX:** Dandelion seeds drifting, gentle grass sway, butterfly flutter  
**Parallax:** 5 layers — distant mountains, forest line, far meadow, near grass, flowers  
**Mood:** "The world is kind and full of possibility"

### 2. Moonlit Creek
**Palette:** `#2C4A6E` deep blues, `#7EC8E3` water shine, `#FFE066` moonlight gold  
**Atmosphere:** Magical, reflective, peaceful evening  
**Lighting:** Soft moonlight from upper-right, caustic water reflections  
**VFX:** Water ripples (WaterRippleSystem), firefly glows, floating lily pads  
**Parallax:** Moon through trees, distant fog, near reeds  
**Mood:** "Magic is real if you're quiet enough to hear it"

### 3. Elderwood Grove
**Palette:** `#1A4D2E` deep greens, `#8B6914` ancient bark, mossy `#5B8A60`  
**Atmosphere:** Ancient, wise, cathedral-like with light beams  
**Lighting:** Rays of light through canopy gaps (god rays particle system)  
**VFX:** Floating dust motes, falling leaves, glowing mushrooms  
**Parallax:** Ancient trees so wide they become the walls, roots, undergrowth  
**Mood:** "You are small, and the world is ancient, and that is wonderful"

### 4. Crystal Caverns
**Palette:** `#1A1A3E` dark indigo, `#7B9FFF` crystal blue, `#E8A838` amber glow  
**Atmosphere:** Mysterious underground wonder, Luma's home  
**Lighting:** Crystal bioluminescence (multiple point lights), no natural light  
**VFX:** Crystal refraction sparkles, dripping water sounds, glowing minerals  
**Parallax:** Crystal formations at 3 depths, stalactites  
**Mood:** "Hidden beauty exists where others are afraid to look"

### 5. Forgotten Ruins
**Palette:** `#5A4A3A` warm stone, `#9AAEA0` weathered gray, moss `#4A6A45`  
**Atmosphere:** Melancholy beauty, history, curiosity  
**Lighting:** Dappled sun through broken ceiling, warm late afternoon  
**VFX:** Crumbling dust, vine sway, ancient rune glow  
**Parallax:** Broken columns, overgrown archways, sky visible through gaps  
**Mood:** "Something wonderful was here before, and we can learn from it"

### 6. Firefly Marsh
**Palette:** `#0D2A1E` dark marsh, `#4A7A50` marsh greens, `#FFE066` firefly gold  
**Atmosphere:** Ethereal, dreamlike, teeming with gentle life  
**Lighting:** Hundreds of firefly lights (FireflyTrailSystem), no hard light sources  
**VFX:** Dense firefly trails, water reflection glows, rising mist  
**Parallax:** Silhouetted reeds, floating lily pads, distant will-o'-wisps  
**Mood:** "The dark is not to be feared when you carry your own light"

### 7. Ancient Observatory
**Palette:** `#0A0A1E` deep night, `#C0C8FF` starlight, `#FFE066` telescope brass  
**Atmosphere:** Majestic, scientific wonder, the cosmos is close  
**Lighting:** Stars as light sources (point lights), constellation glow  
**VFX:** Star particles, telescope lens flare, comet trail, nebula shimmer  
**Parallax:** Dome opening to stars, mechanical brass gears, star charts  
**Mood:** "The universe is vast and you are perfectly placed to explore it"

### 8. Skyroot Canopy
**Palette:** `#B8E0FF` high altitude blue, `#E8D8A0` cloud cream, `#52B788` canopy green  
**Atmosphere:** Above the world — light, free, bird's-eye perspective  
**Lighting:** Bright direct sun, cloud shadows moving across  
**VFX:** Wind-blown leaves, cloud wisps, sun glitter  
**Parallax:** Clouds below, canopy level, sky above  
**Mood:** "From high enough, all problems look small"

### 9. Hidden Druid Sanctuary
**Palette:** `#2D4A2A` deep forest, `#8B6A2A` ancient oak, `#E8C878` druid gold  
**Atmosphere:** Sacred, ancient, whispering with ritual memory  
**Lighting:** Ritual fire glow (warm orange-amber), soft ambient green  
**VFX:** Ritual smoke particles, glowing runes, ember rise  
**Parallax:** Stone circle, ancient trees forming natural walls  
**Mood:** "Some places remember everything"

### 10. Endless Dream Forest
**Palette:** Shifts through all biome palettes in a slow cycle  
**Atmosphere:** Surreal, everything is possible here, the final region  
**Lighting:** Impossible — all colours of light simultaneously, shifting  
**VFX:** All particle systems at once but harmonised, dreamlike bloom  
**Parallax:** Fragments of all other biomes float and drift  
**Mood:** "This is the place at the edge of all stories, where new ones begin"

---

## Shader Recommendations

| Effect | Shader | Notes |
|--------|--------|-------|
| Character outline | 2D outline shader | 3px forest green, 1px inner |
| Glow creatures | Additive sprite blend | Luma, firefly marsh |
| Water surface | UV distortion + normal map | Moonlit Creek, marsh |
| Crystal refraction | Grab pass refraction | Crystal Caverns |
| Seasonal colour grade | Full-screen post LUT | Seasonal palette shifts |
| Reduced motion safe | All shaders must be disable-able | Accessibility requirement |

---

## Animation Timing Reference

| Action | Frames (24fps) | Easing |
|--------|---------------|--------|
| Button tap | 4 frames | Bounce out |
| Card flip | 8 frames | Ease in-out |
| Panel open | 12 frames | Ease out (overshoot 5%) |
| Achievement reveal | 20 frames | Spring (2.5 bounce) |
| Evolution reveal | 60 frames | Custom cinematic curve |
| Region unlock | 40 frames | Ease in → burst |
| Creature blink | 3 frames open→close, 4 frames close→open | Linear |
| Creature idle bob | 120 frames loop | Sine wave |
