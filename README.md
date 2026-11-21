# CCK Advanced

An enhanced distribution of the **ChilloutVR CCK** (Creator Companion Kit) with a full
VRChat-to-ChilloutVR avatar conversion suite, bundled under `Assets/CVR.CCK/Advanced`.

> **Important:** This package *contains the entire CCK*. Do **not** also install the
> official ChilloutVR CCK — remove any existing `Assets/CVR.CCK` folder first.

## Features

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
- **YAPS** — squash & stretch, plugs, sockets, rings
- **Ask me to report problems** — pre-filled diagnostics for bug reports
- **Check for Updates** — GitHub release checker (runs once per day)
- **Credits**

### Worlds — `ChilloutVR CCK/Advanced/Worlds`

- Reserved for upcoming VRC World → CVR conversion tools

## Installation

### VCC / ALCOM (recommended)

Add this listing to your package manager:

```
https://vcc.vrcband.com/index.json
```

Then install **CCK Advanced**. Requires Unity **2021.3.19+** / **2022.3 LTS**.

### Manual

Download `com.fttristan.cck-advanced.zip` from [Releases](https://github.com/FTTristans-Creations/CCKAdvanced/releases/latest)
and extract so that `Assets/CVR.CCK` lands in your project root. A `.unitypackage`
artifact is also attached to each release for legacy installs.

## Credits

| Component | Creator | License |
|---|---|---|
| CCK Advanced | [FTTristan](https://github.com/FTTristans-Creations) | [CC BY-NC-SA 4.0](LICENSE) |
| AvatarBridge (bundled, incl. YAPS) | [MrTactical](https://github.com/MrTactical/AvatarBridge) | MIT |
| ChilloutVR CCK | [ChilloutVR Team](https://chilloutvr.net) | ChilloutVR CCK terms |
| Harmony (vendored, editor-only) | Andreas Pardeike / Harmony | MIT |
| Dynamic Bones Stub (vendored) | [VRLabs](https://github.com/VRLabs/Dynamic-Bones-Stub) | MIT |

See [THIRD-PARTY-NOTICES](Assets/CVR.CCK/Advanced/THIRD-PARTY-NOTICES.md) inside the
package for full license texts and attributions.

## License

Original CCK Advanced code is licensed under
[Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International](LICENSE).
Bundled third-party components remain under their own licenses — see
[THIRD-PARTY-NOTICES](Assets/CVR.CCK/Advanced/THIRD-PARTY-NOTICES.md).
