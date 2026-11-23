# Architecture

## Scene and assets
`Prototype_01.unity` is a serialized, editable scene. The editor-only `PrototypeBuilder` generates it from `WorldBuilder` and saves procedural meshes, materials, URP settings and unit/building prefabs as real Unity assets. Gameplay does not regenerate the map on entering Play. `GameConfig` is a ScriptableObject shared by the systems.

## Components
| Module | Responsibility |
|---|---|
| Core | Session lifecycle, explicit milestones/notifications; deterministic seeded authoring factories |
| Camera | Pan, orbit, orthographic zoom and map focus |
| Units | Unit identity/faction/health; selection; movement; grid A* |
| Combat | Target acquisition, attack commands, range and attack cooldown |
| Construction | Placement preview; territory/cost/collision validation; obstacle registration |
| Economy | Local inventory, finite resource nodes, worker task state machine |
| Outposts | Presence-based capture, contested state, ownership transitions |
| Logistics | Source/destination route state, actual cargo, stops, salvage, escort |
| Ecology | Corpse records/actions; periodic scent aggregation and wolf behaviour |
| UI | Screen-space RTS HUD, world health/capture indicators and interactive minimap |
| Tests | Opt-in player integration scenario; output is persisted on completion |

## Data flow
Worker → ResourceNode.Harvest → worker cargo → ResourceStore.Add.

TradeRoute → source.Take → caravan cargo → movement → destination.Add.

CombatAgent → Unit.Damage → CorpseSystem.Create → Corpse.Influence → EcologySystem.TickEcology → existing wolf commands.

Construction validates territory and available inventory before mutating stock. It rebuilds navigation after the building is created. Ownership is checked again by workers and routes during operation.

## Navigation
128×96 m map, 2 m grid. Diagonal A* does not cut blocked corners. The river is blocked outside the bridge. Building footprints and a reservation for the trade road prevent invalid construction. Failed paths retry every two seconds; a grid revision causes active paths to rebuild. Nearby blocked destination cells resolve to a reachable nearby cell. Buildings are static obstacles; unit avoidance, collision-aware formations and decorative vegetation obstacles are not implemented.

## Extensibility and networking
Gameplay lives outside HUD. `Unit.Move`, `CombatAgent.Attack`, `WorkerAgent.Gather/Clean`, `TradeRoute.SetRoute`, `Corpse.Apply` are explicit command entry points suitable for later authority validation. The current single-player implementation uses Unity references and Time.time, and is **not deterministic lockstep/network-ready**. A multiplayer milestone must add stable entity IDs, authoritative simulation clock/commands, serialization, ownership validation, replication and replay tests. No networking package is included.

`IEcologyProcess.TickEcology` defines an extension point; the current EcologySystem implements it for scent/predators. A future scheduler can compose decomposition/pollution/cursed zones without adding per-frame corpse scripts. Current ecological scans are periodic; combat target scans run at 0.35-second intervals.

## Persistence and performance boundaries
Remains are persistent within one match only. Restart clears the scene. No save format exists yet. Procedural palette materials are shared; the scene contains modest numbers of units but many decorative primitives. Profiling at RTS army scale is future work. IMGUI is intentionally an interim HUD, to be replaced by a scalable UI system.
