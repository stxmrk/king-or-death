# Architecture

## Scenes and assets
`MainMenu.unity` (first in the build) holds the UI Toolkit shell; `Prototype_01.unity` is the serialized, editable game scene. The editor-only `PrototypeBuilder` generates the game scene from `WorldBuilder` and saves procedural meshes, materials, URP settings and unit/building prefabs as real Unity assets; it also adds the HUD and pause-menu documents. Gameplay does not regenerate the map on entering Play. `GameConfig` is a ScriptableObject shared by the systems.

Generated content lives in the project, the generators in `tools/`:

| Content | Where | Made by |
|---|---|---|
| Menu key art, results art, scenario card | `Assets/_Game/Art/UI/*.png` | `tools/art_gen.py` (kie.ai GPT Image 2) |
| Unit portraits, resource and command icons (256 px) | `Assets/_Game/Art/UI/Icons/` | `tools/art_gen.py Icons/` |
| Emblem, frame corner, divider (gold keyed to alpha), panel texture (mirror-tiled) | `Assets/_Game/Art/UI/Ornament/`, `Texture/` | `tools/art_gen.py Ornament/ Texture/` |
| Cursors | `Assets/_Game/Resources/Cursors/` | drawn with Pillow (imported as Cursor textures) |
| UI clicks, alert ping | `Assets/_Game/Resources/Audio/ui_*.ogg` | `tools/audio_gen.py ui` (numpy synthesis) |
| Stingers, ambience, menu and match music | `Resources/Audio/sting_*`, `amb_*`, `music_*` | `tools/audio_gen.py suno` (kie.ai Suno V6) |
| Narrator and advisor lines, EN and RU | `Resources/Audio/vo_<line>_<en|ru>.ogg` | `tools/audio_gen.py voice` (ElevenLabs v3 via kie.ai) |

Audio is trimmed, faded and loudness-matched to OGG; music is imported as streaming. Raw Suno takes stay in the ignored `tools/.audio_raw` and can be re-picked with `audio_gen.py raw name:take`.

## Components
| Module | Responsibility |
|---|---|
| Core | `GameSession`: match clock, speed, log (`Notify` with optional position), announcements, milestones, stats, victory/defeat, autosave. `SaveSystem`: JSON snapshots and thumbnails. Seeded authoring factories |
| Camera | Pan (keys, middle mouse, screen edges), orbit, orthographic zoom, map focus |
| Units | Unit identity/faction/health and the `Damaged` event; selection; movement; grid A* |
| Combat | Target acquisition, attack commands, range and attack cooldown |
| Construction | Placement preview; territory/cost/collision validation; obstacle registration |
| Economy | Local inventory, finite resource nodes, worker task state machine (state is a Loc key) |
| Outposts | Presence-based capture, contested state, ownership transitions |
| Logistics | Source/destination route state (Loc key), cargo, stops, salvage, escort |
| Ecology | Corpse records/actions; periodic scent aggregation and wolf behaviour |
| UI | UI Toolkit shell, HUD, localization, settings, audio (see below) |
| Tests | EditMode NUnit (logic, UXML wiring, translations, assets) and the opt-in `-verify` player scenario |

## User interface
All screens are UI Toolkit documents in `Assets/_Game/UI` styled by `Shell.uss` (shared) and `Hud.uss`:

| Document | Controller | Contents |
|---|---|---|
| `MainMenu.uxml` | `MainMenu` | main menu with continue card, scenario select, load screen, codex, settings, about, loading screen, embers, fader |
| `PauseMenu.uxml` | `PauseMenu`, `BriefingView` | mission briefing (fresh matches only), pause panel, save and load screens, results (victory/defeat) screen, fader; also F5/F9 |
| `Hud.uxml` | `Hud` | top bar, event log, objectives and threats, announcements, tooltips, quick-select bar and control-group tiles, selection panel, command card, caravan panel, minimap (left: camera, right: move order), world overlay |
| `Settings.uxml`, `SaveSlots.uxml`, `Confirm.uxml` | `SettingsView`, `SaveSlotsView`, `ConfirmDialog` | templates reused by the main and pause menus |

- **Localization.** `Loc` holds an EN/RU table. UXML text holds keys, which `Loc.Apply` translates (and re-translates on a language switch); code uses `Loc.T`, `Loc.F`, `Loc.Name` (place and unit names stay English ids in data and saves). Element `tooltip` attributes are keys too.
- **Scaling.** `UiFx.UsePanel` gives every document one runtime copy of `PanelSettings`, so the interface-scale option never edits the asset; on screens narrower than 16:9 the panel matches width instead of height. The HUD switches to a compact layout below 1500 logical pixels, the main menu to a short one below 800.
- **Effects.** `UiFx`: gilded corners on `.ornate` panels, `Reveal`/`Stagger` entrances, scene fades, cursors, the `EmberField` particle element. `HudOverlay` draws selection and hover rings, health bars and order markers with Painter2D; `HudMinimap` shows a top-down render baked once at start.
- **Audio.** `UiAudio` is one persistent object: effects, voice (ducks the music), ambience and two crossfading music sources. `UiAudio.Wire(root)` gives every button hover and click sounds chosen by its style class. Announcements play a stinger and then the advisor line.

## Data flow
Worker → ResourceNode.Harvest → worker cargo → ResourceStore.Add.

TradeRoute → source.Take → caravan cargo → movement → destination.Add.

CombatAgent → Unit.Damage (→ `Unit.Damaged` → HUD numbers, under-attack warning) → CorpseSystem.Create → Corpse.Influence → EcologySystem.TickEcology → wolf commands.

Simulation → `GameSession.Notify` (log, minimap ping, camera jump) and `GameSession.Announce` (headline, stinger, voice) → HUD. `GameSession.Ended` → results screen.

Construction validates territory and available inventory before mutating stock. It rebuilds navigation after the building is created. Ownership is checked again by workers and routes during operation.

## Navigation
128×96 m map, 2 m grid. Diagonal A* does not cut blocked corners. The river is blocked outside the bridge. Building footprints and a reservation for the trade road prevent invalid construction. Failed paths retry every two seconds; a grid revision causes active paths to rebuild. Nearby blocked destination cells resolve to a reachable nearby cell. Buildings are static obstacles; units separate softly; collision-aware formations and decorative vegetation obstacles are not implemented.

## Persistence
`SaveSystem` writes `persistentDataPath/saves/<id>.json` (`autosave` every 3 minutes, `quicksave` on F5, `save_<timestamp>` from the pause menu) plus a `<id>.png` thumbnail rendered off-screen from the game camera. A snapshot holds stores, post capture state, units (position, health, worker cargo and gathering node, caravan route and cargo, escort), resource nodes and salvage, built houses, remains (age and state), milestones, stats, camera and the victory flag. Loading reloads the game scene and applies the snapshot from `GameSession.Start`. Files are written to a temp file and swapped; unreadable or other-version files are skipped. Unit identity is not stored, so attack targets and corpse-cleanup jobs restart idle after a load.

## Extensibility and networking
Gameplay lives outside the HUD. `Unit.Move`, `CombatAgent.Attack`, `WorkerAgent.Gather/Clean`, `TradeRoute.SetRoute`, `Corpse.Apply` are explicit command entry points suitable for later authority validation. The single-player implementation uses Unity references and Time.time, and is **not deterministic lockstep/network-ready**. A multiplayer milestone must add stable entity IDs, an authoritative simulation clock and commands, ownership validation, replication and replay tests. No networking package is included.

`IEcologyProcess.TickEcology` defines an extension point; the current EcologySystem implements it for scent/predators. Current ecological scans are periodic; combat target scans run at 0.35-second intervals. Procedural palette materials are shared; profiling at RTS army scale is future work.
