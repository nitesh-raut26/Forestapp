# Forest Friends Quest — Content Pipeline

## Content Categories

| Category | Count | Source | Update Cadence |
|----------|-------|--------|---------------|
| Puzzle definitions | 200+ | `ForestGameContent.cs` | Major updates |
| Creature dialogue | ~300 lines/creature | `DynamicDialogueSystem` | Minor updates |
| Lore pages | 50+ | `EnvironmentalStorySystem` | Seasonal |
| Sanctuary items | 30 (base) + IAP | `SanctuaryDecorationCatalog` | Minor updates |
| Daily rituals | 25+ | `DailyRitualSystem` | Seasonal |
| Seasonal events | 12/year | `SeasonalEventSystem` | Monthly |
| Boss encounters | 10 | `BossEncounterSystem` | Major updates |
| Biome backgrounds | 10 | Art pipeline → Addressables | Major updates |
| Character sprites | 6 creatures × 8 states | Art pipeline → Addressables | Major updates |
| Audio tracks | ~40 | Audio pipeline → Addressables | Minor updates |
| Voice lines | ~150 | Voice pipeline → Addressables | Major updates |

---

## Addressables Pipeline

### Bundle Structure

```
Addressables/
├── Core/
│   ├── core_ui          [boot, never unload]
│   ├── core_audio       [boot, never unload]
│   └── creatures        [boot, never unload]
├── Biomes/
│   ├── biome_ferntrail  [zone-streamed]
│   ├── biome_firefly    [zone-streamed]
│   ├── biome_river      [zone-streamed]
│   └── ...              [one per biome]
├── Seasonal/
│   ├── spring           [seasonal, hot-swap]
│   ├── summer
│   ├── autumn
│   └── winter
└── IAP/
    ├── starter_pack     [unlocked on purchase]
    ├── premium_decor    [unlocked on purchase]
    └── lore_packs/
        ├── druid
        └── ancient
```

### Adding New Content

1. Place asset in `Assets/Content/[Category]/[Name]`
2. In Addressables Groups window: assign to correct group
3. Set Address key matching `ContentBundle.resourcePath`
4. Update `AddressableContentManager.RegisterCoreBundles()` if new group
5. Rebuild Addressables: `Window → Addressables → Build → New Build`
6. Upload to CDN; update catalog hash

---

## Art Asset Specifications

### Character Sprites

| Character | Sheet Size | Frames |
|-----------|------------|--------|
| Pip the Fox | 1024×1024 | 8 states × 8 frames = 64 |
| Mimi the Bird | 1024×1024 | 8 states × 8 frames = 64 |
| Tomo the Turtle | 1024×1024 | 8 states × 6 frames = 48 |
| Luma the Firefly | 512×512 | 8 states × 12 frames = 96 |
| Nori the Deer | 1024×1024 | 8 states × 8 frames = 64 |
| Sol the Owl | 1024×1024 | 8 states × 8 frames = 64 |

Import settings:
```
Texture Type: Sprite (2D and UI)
Sprite Mode: Multiple
Filter Mode: Bilinear
Max Size: 1024 (Tier 1), 2048 (Tier 2+)
Compression: ASTC 6×6 (iOS/Android high), ETC2 (Android low)
```

### Biome Backgrounds

| Layer | Resolution | Parallax Speed |
|-------|-----------|---------------|
| Sky / atmosphere | 2048×1024 | 0.05x |
| Far background | 2048×1024 | 0.1x |
| Mid trees | 2048×1024 | 0.2x |
| Near foliage | 2048×1024 | 0.5x |
| Ground / foreground | 2048×512 | 1.0x |

All layers must tile seamlessly horizontally.
PSD/Aseprite source files kept in `Art/Source/[BiomeName]/`.

### UI Elements

- Reference resolution: 1080×1920
- Button minimum size: 88×88 px (44dp at 2x)
- Font: custom rounded sans-serif (or system default in prototype)
- All UI uses 9-slice sprites for resolution independence

---

## Audio Pipeline

### Music

- Format: OGG Vorbis, quality 7 (~128 kbps)
- Sample rate: 44100 Hz (Tier 2+), 22050 Hz (Tier 1)
- Looping: all music tracks loop seamlessly
- Stems available for `DynamicAmbientMixer` (4-layer stem system)
- Naming: `music_[biome]_[mood]_[stem].ogg`
  - `music_meadow_calm_melody.ogg`
  - `music_meadow_calm_harmony.ogg`
  - `music_meadow_calm_rhythm.ogg`
  - `music_meadow_calm_bass.ogg`

### SFX

- Format: WAV (editor), OGG (runtime)
- Bit depth: 16-bit, mono (SFX), stereo (music)
- Normalised to -3 dBFS peak
- Naming: `sfx_[category]_[action]_[variant].wav`
  - `sfx_ui_button_tap_01.wav`
  - `sfx_creature_pip_happy_01.wav`
  - `sfx_puzzle_complete_fanfare.wav`

### Voice Lines

See [VOICE_PRODUCTION_GUIDE.md](VOICE_PRODUCTION_GUIDE.md) for full pipeline.

Format for import:
- WAV 24-bit 44100 Hz (raw recording)
- OGG Vorbis quality 8 (runtime distribution)
- Naming: `vo_[creature]_[context]_[variant].ogg`
  - `vo_pip_greeting_morning_01.ogg`
  - `vo_tomo_puzzle_hint_01.ogg`

---

## Localisation Pipeline

10 supported languages: EN, ES, FR, DE, JA, KO, PT, IT, NL, ZH

### String Format

Strings stored in `Assets/Resources/Localization/[lang].json`:
```json
{
  "ui.menu.play": "Play",
  "ui.menu.parent": "Parent Dashboard",
  "creature.pip.greeting.morning": "Good morning, explorer!",
  "puzzle.hint.button": "Need a hint?"
}
```

### Font Requirements

| Language | Required Font |
|---------|--------------|
| EN/ES/FR/DE/PT/IT/NL | Rounded sans-serif (Latin) |
| JA | Noto Sans JP |
| KO | Noto Sans KR |
| ZH | Noto Sans SC |

Dynamic font atlas generation via Unity's TextMeshPro is required for
CJK characters. Pre-bake glyph atlases for the most common 3000 characters.

### Pseudo-Localization Testing

Enable pseudo-localization in `LocalizationManager` to:
- Double string lengths (test layout overflow)
- Add accented characters (test font coverage)
- Add RTL markers (test bidirectional layout)
