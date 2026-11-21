# Creating a Custom Locale for the CCK

The CCK loads editor translations from `CCKLocale` ScriptableObjects, which reference structured JSON files with localized strings. They are then used by any supported GUI to localize it for the selected language.

---

## How It Works

- `CCKLocale` assets define:
  - A **name** (identifier), which appears in the CCK language dropdown.
  - A reference to a **JSON TextAsset** containing structured localized strings.
- Keys with `-TOOLTIP` provide tooltips for fields.
- Locales are loaded from any path under a `Resources/` folder.

---

## Example JSON

```json
{
  "CCKLocale": {
    "ContentBuilder": {
      "BUILD_UPLOAD": "UwU Upload",
      "BUILD_UPLOAD-TOOLTIP": "Nyaa~ your content goes here!"
    }
  }
}
```

The first key must be `"CCKLocale"`.

---

## Creating the Asset

1. In Unity: `Right-click → Create → CCK → Locale Asset`
2. Set the **Identifier** to your desired language name (e.g., `OwO`)
3. Assign your JSON TextAsset to the "Locale File" field

---

## Placement Guide

The CCK will only detect `CCKLocale` assets in `Resources` folders. If it's not in a `Resources` folder, it will be ignored.

**Do (valid):**

* `Assets/Editor/Resources/CCK_OwO.asset`
* `Assets/SuperCoolStuff/Resources/CCK_Pirate.asset`

**Don't (invalid):**

* `Assets/MyLocales/CCK_OwO.asset`
* `Assets/Editor/CCK_OwO.asset`

---

## Duplicate Identifiers

TODO

---

## Feedback:
If you run into any issues making locales or have suggestions for potential improvements to this system please feel free to suggest them on our issue tracker: https://github.com/ChilloutVR-Team/ChilloutVR-Issues/issues.