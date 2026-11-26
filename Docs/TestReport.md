# Prototype verification report

Unity 6000.6.10f1 / URP 17.6.0 / Windows x64 Mono. Final player integration run: **27 PASS, 0 FAIL**. EditMode NUnit suite (`Assets/_Game/Tests/EditMode`): **17 PASS** (simulation logic plus menu and HUD UXML wiring and EN/RU translation). The `-verify` run now boots through the MainMenu scene, which skips straight to the scenario. See [individual assertions](RuntimeVerification.txt).

## Executed checks
The actual standalone player ran an accelerated scenario through production gameplay components. Verified: initial units; rectangle selection; A* bridge restriction; flag capture and contested progress; valid/invalid building placement; unchanged construction radius; worker harvesting and local delivery; no resource teleportation; caravan loading, travel and unloading; empty-source waiting and resumption; melee death and persistent remains; scent delay and wolves physically leaving the den; worker burial; insufficient stock; enemy recapture; route suspension on ownership loss; salvage of half the cargo; complete route blockade and recovery.

The recapture fixture disables automatic combat on its enemy soldier to keep it inside the flag radius. This isolates capture logic from chasing nearby workers; it does not validate enemy tactical decision-making. Some test fixtures are spawned at controlled positions. Only the tested transport/movement steps establish physical travel.

## Manual Windows UI checks
Observed in the running player: army selection with 1; right-click movement and resulting Pineward capture; single worker click; four-worker drag selection; red placement preview outside territory; successful house placement from UI with stock changing 220/120 to 170/100; camera wheel zoom. Corrected fast-drag input loss using per-event pointer positions. Pause/restart buttons exist but were not manually exercised in this run.

## Build and assets
Windows build completed successfully. No C# compilation errors and no runtime exceptions in the final integration log. Unity engine/package informational/deprecation diagnostics are not a claim of zero warnings. Scene, material and prefab assets are serialized with .meta files. Repaired house prefab material persistence on regeneration and verified non-null material references. Unity-generated scene and player screenshots are in Screenshots.

## Limits
No long-duration soak, hundreds-unit benchmark, Unity Profiler recording, network test, WebGL build or cross-machine test. Rendering is an original procedural low-poly prototype, not production-quality stylized realism. Unit avoidance is soft O(n²) separation, not yet profiled at 100–300 units. See README for remaining gameplay and visual limitations.
