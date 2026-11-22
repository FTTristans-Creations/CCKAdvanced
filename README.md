# CCK Advanced

An enhanced distribution of the **ChilloutVR CCK** (Creator Companion Kit) with a full
VRChat-to-ChilloutVR avatar conversion suite, bundled under `Assets/CVR.CCK/Advanced`.

> **Install via the CCK Advanced Installer — not the raw package.** Install the
> installer once and you never have to update CCK Advanced yourself again — it
> downloads, installs, and keeps `Assets/CVR.CCK` up to date automatically.
> Installing the CCK files directly means you will **not** receive updates.
>
> **Important:** This distribution *contains the entire CCK*. Do **not** also install
> the official ChilloutVR CCK — remove any existing `Assets/CVR.CCK` folder first.

## Features

Everything lives under the **`ChilloutVR CCK/Advanced/`** menu.

### Avatars — `ChilloutVR CCK/Advanced/Avatars`

- **VRChat to ChilloutVR Converter** (bundled [AvatarBridge](https://github.com/MrTactical/AvatarBridge) by MrTactical)
  - View & voice position, eye look, blinking, lip sync/visemes
  - Expression parameters & menus (ports everything that fits the CCK's 3200-bit budget)
  - PhysBones → DynamicBone/MagicaCloth2, contacts, constraints
  - Animator merging, locomotion, VRCFury baking, Avatar Scale Tool settings
  - YAPS penetration support, face tracking, diagnostics & pre-conversion reports
- **Avatar Tester** — in-editor avatar testing utilities

### Common — `ChilloutVR CCK/Advanced/Common`

- **ChilloutVR Toolkit** — utilities for any CVR avatar
- **YAPS** — squash & stretch, plugs, sockets, rings (setup windows, prefab
  creators, shader patching tools)
- **Ask me to report problems** — pre-filled diagnostics for bug reports

### Advanced root

- **Check for Updates** — GitHub release checker (runs once per day)
- **Credits**

### Worlds — `ChilloutVR CCK/Advanced/Worlds`

- Reserved for upcoming VRC World → CVR conversion tools

## Repository layout

| Path | Contents |
|---|---|
| `Assets/CVR.CCK/` | The full CCK + Advanced content. `Advanced/Editor/` holds all editor tooling; `Advanced/Runtime/` holds the few scripts that must exist in player builds (CCKAdvancedInfo, YAPS components). |
| `Assets/csc.rsp` | Response file shipping the `CVR_CCK_*` compile defines so fresh projects compile on the first try. |
| `installer/com.fttristan.cck-advanced.installer/` | Source of the installer package distributed via VPM/VCC. |
| `.github/workflows/release.yml` | Builds the release payload zip, installer zip + unitypackage, and CCK unitypackage. |

## Installation

### VCC / ALCOM (recommended — the only supported method)

Add this listing to your package manager:

```
https://vcc.vrcband.com/index.json
```

Then install **CCK Advanced Installer** (`com.fttristan.cck-advanced.installer`).

On the next editor launch the installer downloads the latest CCK Advanced release
and installs it into `Assets/CVR.CCK` — after that it keeps itself up to date
automatically, so you never have to update it yourself again. Requires Unity
**2021.3.19+** / **2022.3 LTS**.

To trigger a check manually: `ChilloutVR CCK/Advanced/Common/Check for CCK Advanced Updates`.

### Why the installer?

CCK Advanced ships as plain `Assets/` content so its scripts live in
`Assembly-CSharp`/`Assembly-CSharp-Editor` — the same assemblies the official CCK
uses, which third-party tools (e.g. ThryEditor integrations) rely on. Because it is
not a versioned UPM package, the installer handles downloading, manifest-tracked
updates (files removed upstream are deleted locally, your own files are untouched),
and merging `Assets/csc.rsp` so the required compile defines always exist.

### Unsupported: manual installs

The release page also attaches `com.fttristan.cck-advanced-installer-*.unitypackage`
for projects without a package manager — import it and the installer does the rest.
Raw CCK payloads and `cck-advanced-source-*.zip` snapshots are provided for
inspection/backup only; installing them directly is **not supported** and will not
auto-update.

## Credits

| Component | Creator | License |
|---|---|---|
| CCK Advanced | [FTTristan](https://github.com/FTTristans-Creations) | [CC BY-NC-SA 4.0](LICENSE) |
| AvatarBridge (bundled, incl. YAPS) | [MrTactical](https://github.com/MrTactical/AvatarBridge) | MIT |
| ChilloutVR CCK | [ChilloutVR Team](https://chilloutvr.net) | ChilloutVR CCK terms |
| Harmony (vendored, editor-only) | Andreas Pardeike / Harmony | MIT |

See [THIRD-PARTY-NOTICES](Assets/CVR.CCK/Advanced/THIRD-PARTY-NOTICES.md) inside the
package for full license texts and attributions.

## License

Original CCK Advanced code is licensed under
[Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International](LICENSE).
Bundled third-party components remain under their own licenses — see
[THIRD-PARTY-NOTICES](Assets/CVR.CCK/Advanced/THIRD-PARTY-NOTICES.md).
