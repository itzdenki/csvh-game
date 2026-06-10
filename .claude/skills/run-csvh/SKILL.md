---
name: run-csvh
description: Run, play, drive, and screenshot the CSVH (Cổ Sử Việt Hùng) Unity tower-defense game. Use when asked to launch/start/play the game, enter Play mode, take a screenshot of the running game, inspect live game state, or run its tests — all via the UnityMCP server against the open Editor.
---

# Run CSVH — Cổ Sử Việt Hùng (Unity 6 tower-defense)

CSVH is a Unity 6 (`6000.4.8f1`) URP-2D tower-defense game. There is **no
shell command that "runs" it** — the app is the Unity Editor playing
`Assets/Scenes/SampleScene.unity`. You drive it through the **UnityMCP**
MCP server (tools prefixed `mcp__UnityMCP__`) against an already-open
Editor. The "driver" is the ordered MCP tool sequence below; the live-state
probe is `.claude/skills/run-csvh/inspect_state.cs`.

Paths in this file are relative to the repo root (`D:\Unity\csvh`).

## Prerequisites

- **Unity Editor open on this project with the UnityMCP bridge connected.**
  This is the one hard requirement — every step talks to that live Editor.
  Verify with the `mcpforunity://instances` resource; you want one instance
  like `csvh@<hash>` reporting `unity_version 6000.4.8f1`. If zero
  instances, the Editor isn't open / the bridge isn't running — ask the
  user to open the project in Unity Hub. You cannot launch it from a shell
  here.
- No `apt-get` / packages needed — this is a Windows host with the Editor
  already installed, not a headless Linux container.

## Run (agent path) — drive the live Editor via UnityMCP

Do these as MCP tool calls, in order. Each was run this session and worked.

1. **Confirm the Editor is ready.** Read resource
   `mcpforunity://editor/state`. Require `advice.ready_for_tools == true`,
   `compilation.is_compiling == false`, and
   `play_mode.is_playing == false`. (The legacy URI `mcpforunity://editor_state`
   does **not** exist — use `editor/state` with the slash.)

2. **Confirm the scene + composition root.**
   `manage_scene(action="get_active")` → expect `SampleScene`.
   `manage_scene(action="get_hierarchy", max_depth=1)` → expect roots
   `GameSceneRoot`, `Tower` (TowerView), `EnemySpawner` (EnemySpawnerView),
   `HUD` (UIDocument + HUDController), `Input`, `Audio`, `Main Camera`,
   `Global Light 2D`, `GroundGrid`. `GameSceneRoot` is the single entry point
   (composition root).

3. **Clear then enter Play mode.**
   `read_console(action="clear")`, then `manage_editor(action="play")`.
   Returns `"Entered play mode."` Re-read `mcpforunity://editor/state` until
   `play_mode.is_playing == true` and `play_mode.is_changing == false`
   (the play-mode transition takes a few seconds).

4. **Screenshot the Game view.**
   ```
   manage_camera(action="screenshot", capture_source="game_view",
                 include_image=true, max_resolution=700,
                 output_folder="Assets/Screenshots",
                 screenshot_file_name="csvh_play.png")
   ```
   A correct capture is **widescreen** (~700×347, ~2:1) and shows: the gold
   **Tower** anchored bottom-right (Đông Nam), a dashed white **aim line**
   running to the field edge, and the Vietnamese **HUD** — `Đợt N/∞` (wave),
   `Điểm:` (score), `Cao nhất:` (high score), `Cấp:` (level), `Vòng:` (gold),
   an HP bar (e.g. `175/200`), and upgrade icons bottom-left.
   **Look at the image.** If it shows an editor grid + a gizmo toolbar, you
   captured the Scene View instead — see Gotchas; just call screenshot again.

5. **Inspect live runtime state (programmatic drive).** Paste the body of
   `.claude/skills/run-csvh/inspect_state.cs` as the `code` arg of
   `execute_code(action="execute")`. Expected shape:
   ```
   isPlaying=True timeScale=1
   enemyCount=3
   tower=Tower pos=(8.00, -5.00, 0.00) rotZ=0.0
   spawnerPresent=True
   ```
   `enemyCount` rising over successive calls and `tower.pos == (8,-5,0)`
   confirm the wave loop + FieldGeometry (tower in the Đông Nam corner) are
   live. To confirm the loop progresses, screenshot again after a few
   seconds: wave/score/level climb (observed `Đợt 1→5`, `Điểm 80→280`,
   `Cấp 1→2`).

6. **Check for errors.** `read_console(action="get", count=30,
   types=["error","warning"])`. A clean run returns 0 entries.

7. **Stop cleanly.** `manage_editor(action="stop")` → `"Exited play mode."`

## Test

EditMode = pure-Core logic + FsCheck property tests, fast, no scene:

```
run_tests(mode="EditMode", assembly_names=["CSVH.Tests.Edit"])
```

Returns a `job_id`; poll `get_test_job(job_id=..., wait_timeout=60)`.
Verified: **66/66 passed in ~3.1s**. PlayMode tests
(`CSVH.Tests.Play`) exist too — use `mode="PlayMode"` and a longer
`init_timeout` (≈120000) because of domain reload.

## Direct invocation (no full app)

The game logic lives in `CSVH.Core` (pure C#, no `UnityEngine`). Most logic
PRs (combat math, leveling, upgrades, wave scheduling) are exercised purely
through `CSVH.Tests.Edit` — you do **not** need Play mode for them. Reach for
the Play-mode path only when a change touches the Unity layer (`CSVH.Game`:
views, HUD, input, rendering).

## Gotchas

- **`execute_code` is a method body, not a file.** `using` directives throw
  `Unexpected symbol`. Fully-qualify types instead
  (`CSVH.Game.Spawning.EnemyView`, `CSVH.Game.Tower.TowerView`). Default
  compiler is CodeDom / C# 6.
- **Screenshot can grab the Scene View instead of the Game view.** Seen mid-
  session: a `capture_source="game_view"` call returned a 4:3 image of the
  editor grid with the scene gizmo toolbar. The fix is just to call
  `screenshot` again — the next capture returned the real Game view. Tell
  them apart by aspect (game ≈ 2:1, scene ≈ 4:3) and content (HUD/tower vs.
  grid/gizmos). Capturing right after the play transition settles is most
  reliable.
- **Editor-state resource URI is `mcpforunity://editor/state`** (slash), not
  `editor_state`. The underscore form 404s.
- **`Id`s keep Vietnamese diacritics** in JSON/code (`Hồ_Tinh`,
  `Quân_Tống`); only sprite filenames are ASCII. Expect Unicode in HUD text
  and console.
- **`agent.md` at the repo root is about a different project** (the
  `com.besty.unity-skills` package), not CSVH. Ignore it for running this
  game; the real reference is `README.md`.
- **Config errors show in-game, not as exceptions.** If `Assets/StreamingAssets/
  waves.json` or `enemies.json` is missing/malformed, Play mode shows a
  "Cấu hình lỗi" screen (with line/col) instead of throwing — check the Game
  view, not just the console.

## Troubleshooting

- **`mcpforunity://instances` shows 0 instances / tools error with "multiple
  connected"** → Editor not open or bridge down (0), or pin one with
  `set_active_instance("csvh@<hash>")` (multiple).
- **`editor/state` has `is_compiling=true` or `is_domain_reload_pending=true`**
  → a script just changed; wait and re-read before `play`. Entering Play mid-
  compile is unreliable.
- **Screenshot is blank / shows "Cấu hình lỗi"** → JSON config problem; see
  Gotchas. Fix the StreamingAssets JSON, exit and re-enter Play.
- **PlayMode test job times out initializing** → raise `init_timeout` to
  ~120000; PlayMode needs a domain reload before the first test.

## Human path

Open the project in Unity Hub, open `Assets/Scenes/SampleScene.unity`, press
**Play**. Controls: `←/→` (or `A/D`) aim, `1` buy Armor, `2` buy Attack,
`Z/X/C` special skills, click HUD icons for upgrade panels. Useless without a
display — for automation use the agent path above.
