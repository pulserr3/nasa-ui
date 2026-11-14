# Red Frontier main menu — session report

14 November 2026 · Unity 2022.3.18f1 · project `C:/Apps/Github/Red frontier/Red Frontier`

## 1. Unity MCP connection

| Step | Finding | Result |
| --- | --- | --- |
| First check | `unity-mcp` (configured in `~/.claude.json` as `http://localhost:8080/mcp`) refused connections; nothing listened on port 8080. `unity-editor-mcp` found no Editor with a Pipeline server. | Not connected |
| Server start | Starting the MCP for Unity server from Claude was blocked by the auto-mode permission check. | Started by the user instead |
| Editor link | Server up but 0 Unity instances until the Editor connected through **Window › MCP for Unity**. | Connected: *Session Active (Red Frontier)* |
| Verification | `manage_scene get_active` returned `SampleScene` (build index 1, 1 root object). | Working |

`unity-editor-mcp` uses Pipeline, which needs Unity 6. Red Frontier runs 2022.3.18f1, so that server stays unused.

## 2. Requests during the session

- Implement and verify the main menu from the handoff brief (layout, art, orbit, occlusion, Reduce Motion, launch, cursor, Help).
- Do not use `SatelliteOrbit.png`. The orbit arcs are drawn in Unity instead.
- Use every sprite in `sprites/`, including `DistantBlackHole.png`.
- Use the new cursor art `sprites/Pointers/Cursor1.png` instead of the earlier rocket cursor files.
- Keep compiles to a minimum on a slower PC. Checks ran inside one Play session, with code fixes batched.
- Build WebGL for browser checks (in progress, see section 7).

## 3. Code changes

### `Assets/MainMenu.cs` (rewritten)

- **Layout, wide (1280 × 720 design):** The logo's visible lettering and both buttons share a left edge of 72 and a width of 440. Start Mission sits at y 427 and Controls / Help at y 519, both 72 tall with a 20 px gap. Mars has its centre at (960, 360) with a visible radius of 248.
- **Layout, narrow (500 × 840 design):** The title, Mars with its orbit, and the two buttons stack top to bottom. The left edge is 50 and the width 400. The buttons are at y 636 and 728, and Mars has a visible radius of 158.
- **Visible-bounds constants:** Positions come from the art's visible pixel bounds, not the PNG canvas. For the logo that is left .097, right .905, top .034 and bottom .949. For Mars the visible radius is .448 of the image width.
- **Buttons:**
  - Start Mission is a square coral `#E46C6B` button with dark text.
  - Controls / Help has a charcoal fill inside a 3 px coral outline.
  - A cream ring shows hover and keyboard focus.
  - A nested `MenuButton` darkens the face while it is pressed, and the face stays dark during launch.
- **Sky:** `StarField.png` covers the whole viewport with a centred, aspect-preserving crop (`CoverSky`).
- **Black hole:** `DistantBlackHole.png` is a small, static accent at 60% opacity, in the open sky between the title and Mars.
- **Orbit:**
  - One relay follows an 18-second ellipse, driven by unscaled time, tilted 12° with a 0.18 flattening.
  - It starts near Mars's left edge.
  - The ellipse's long radius is the visible radius plus 32, so the full wingspan clears Mars where the relay changes depth.
  - The relay snaps to whole design pixels so it does not shimmer.
- **Arcs:** A nested `OrbitArc` graphic draws the front and back halves of the same ellipse the relay follows.
- **Draw order:** Sky, black hole, rear arc, relay on its rear pass, Mars, front arc, relay on its near pass, then the title, buttons, Help and the fade. Mars's own silhouette hides the relay on its rear pass, which replaces the old visibility cutoff.
- **Help:**
  - Focus stays inside the panel and is restored if it is lost.
  - Escape (the `Cancel` input) or Back closes the panel and returns focus to Controls / Help.
  - The instructions match the inputs that work: click or tap, arrow keys or WASD, Enter or Space, Esc.
- **Reduce Motion:** The existing `ReduceMotion` preference is kept. When it is on, the menu holds a still frame with the relay on its front-left pass and no star pulse, and Start goes straight to the mission scene.
- **Launch:**
  - Start Mission works from the first frame.
  - A `launching` flag prevents duplicate scene loads.
  - Normal mode fades out over 0.2 s before loading `SampleScene`.
- **Removed:** the Mars drift, the beacon on Mars, and the beacon flashes during launch.

### `Assets/FrontierUi.cs`

- Added a generic `Action<T>()` so the menu can use its own button type. The existing `Action()` calls the generic version, so shared styling is unchanged.

## 4. Assets and import settings

| Asset | Source | Import | Assigned to |
| --- | --- | --- | --- |
| `Assets/Art/MainMenu/MarsRelaySatellite.png` | matches `sprites/` byte for byte | Default, point, no mipmaps, uncompressed RGBA32, Read/Write on, max size 64 (64 × 43, 46 px visible wingspan). Alpha is made binary at runtime to remove the soft fringe. | `satelliteArt` (was the old `Satellite.png`) |
| `Assets/Art/MainMenu/StarField.png` | copied from `sprites/` | Default, point, no mipmaps, uncompressed, full 1672 × 941 | `sky` (new field) |
| `Assets/Art/MainMenu/DistantBlackHole.png` | copied from `sprites/` | Default, point, no mipmaps, uncompressed, Read/Write on, max size 128 | `blackHoleArt` (new field) |
| `Assets/Art/MainMenu/MarsPlanet.png`, `RedFrontierLogo.png` | match `sprites/` | Unchanged | `mars`, `logo` |
| `Assets/Art/RocketCursor.png` | 32 × 32 version of `sprites/Pointers/Cursor1.png` | Cursor, point, max 32, Read/Write on, alpha transparency, RGBA32 uncompressed, no mipmaps. GUID `0f92bccadb6e479ba32077877cca42c3` kept. | Player Settings Default Cursor, hotspot (2, 2) |

The source PNGs in `sprites/` were not modified.

**Cursor notes:**
- The earlier `rocket-cursor-preview.png` was an exact 8× enlargement of the old 32 px cursor, with all cells matching.
- Per your follow-up, the cursor was rebuilt from `Cursor1.png`. That image is 1536 × 1024 with a transparent background, and its art blocks are about 27 px.
- Each half-block became one cursor pixel, chosen by majority colour. The art fills 29 × 30 px, with the tip at (2, 2).
- The cursor's import settings had drifted to Sprite, bilinear and DXT5. They were set back to the required values.

**Unused but kept:** `Satellite.png`, `SatelliteOrbit.png` and `OutpostBeacon.png`.

## 5. Effects decisions

| Effect | Decision | Reason |
| --- | --- | --- |
| Relay orbit with silhouette occlusion | Kept | The main motion |
| Logo star pulse (6 s, faint) | Kept | Stationary and subtle; off under Reduce Motion |
| Mars drift (4 px over 12 s) | Deferred | Sub-pixel movement of the point-filtered planet would shimmer, and the orbit already gives enough motion |
| Distant black hole | Kept, static | You asked to use all sprites; it is small and subdued |
| Beacon cue | Dropped | Two flashes inside a 0.2 s fade did not read |

## 6. Verification in Unity Play Mode

Checks were scripted through MCP inside one Play session. Unity was set to run in the background for that session only; the project setting was not changed.

| Check | Result |
| --- | --- |
| Still frame at 1280 × 720 | Pass: alignment, spacing and orbit clearance as designed |
| Still frame at 390 × 844 | Pass: stacked layout; buttons about 312 × 56 px on screen |
| Orbit phases 0, .25, .5, .94 | Pass: front and rear depth correct; relay clear of Mars at both switches; readable over sky and Mars |
| Loop seam | Pass by construction; the angle is continuous |
| Help focus trap | Pass: focus is restored to Reduce motion after being lost; Start and Help can't be reached |
| Back returns focus | Pass: focus lands on Controls / Help |
| Reduce Motion persistence | Pass: still On after a scene reload; still frame with the relay visible |
| Repeated Start, Reduce Motion | Pass: 4 activations gave 1 load; pressed face held |
| Repeated Start, normal mode | Pass: 3 activations gave 1 load after the fade |
| Launch destination | `SampleScene` holds only Main Camera with AudioListener, so **the mission screen is pending** |
| Console | No errors or warnings |

**Not covered in the editor:**
- Real keypresses: Escape and the arrow keys were exercised through code paths, not physical keys.
- A real mouse: the rocket tip against a button edge wasn't checked by hand.
- Touch input.

**Screenshots** in `MenuScreenshots/`: `wide-1280x720.png`, `narrow-390x844.png`, `wide-reduced-motion.png` and `narrow-help.png`.

**Test side effects:**
- The editor's `ReduceMotion` preference is set to Off.
- Two custom Game view sizes were added: 1280 × 720 and 390 × 844.

## 7. WebGL (in progress)

- A WebGL build to `Builds/WebGL` has started. WebGL support is installed, and the project uses Brotli compression with no decompression fallback.
- **Next:**
  1. Serve the build with Unity's `SimpleWebServer.exe` from `.../2022.3.18f1/Editor/Data/PlaybackEngines/WebGLSupport/BuildTools/`, which handles the Brotli headers.
  2. Open it in the preview browser.
  3. Repeat the resize, cursor and hotspot, keyboard, motion, Reduce Motion and single-launch checks.
- Touch needs a touch-capable browser or device.

## 8. Open items

1. Finish the WebGL browser checks.
2. Hand-test Escape, keyboard navigation and the cursor tip with a real mouse and keyboard.
3. Build the mission screen in `SampleScene`, or confirm another destination.
4. Optionally remove the unused `Satellite.png`, `SatelliteOrbit.png` and `OutpostBeacon.png`.
