# Localization

Key-based editor localization for VPM and Unity packages: every label, button and tooltip is a key, each language is
one asset, and switching the language rewrites the open windows immediately.

[Documentation](https://t2penbix99wcoxkv3a4g.github.io/VPM-Docs/#/localization/Overview)

## Installation

The package is `io.github.ykysnk.localization`, distributed through the repository at
<https://t2penbix99wcoxkv3a4g.github.io/VPM/>. Add that listing to ALCOM or the VPM CLI, then install the package.

It depends on `io.github.ykysnk.utils`, which VPM resolves automatically. Unity 2022.3 or newer.

## Quick start

**1. Create the language assets.** Use **Assets > Create > Localization > Basic Localization**, one asset per
language. The file name is the language id (`en-US`, `ja-JP`, `zh-Hans`, `zh-Hant`, ...) and every asset of one
package carries the same **Localization ID**.

**2. Declare the id of your package once per editor assembly.**

```csharp
using io.github.ykysnk.Localization.Editor;

[assembly: LocalizationPackage("com.example.my-package")]
```

**3. Write keys in the UXML and localize the tree before binding it.**

```xml
<ui:Label text="label.my_window.title"/>
<ui:Button text="label.my_window.apply"/>
```

```csharp
var tree = uxml.CloneTree();
tree.UILocalize();          // replaces the keys and keeps them current
tree.Bind(serializedObject);
rootVisualElement.Add(tree);
```

**4. Look keys up in code** where the text is built at runtime:

```csharp
label.text = "label.my_window.title".S();
```

A missing entry never throws: the lookup walks the selected language, then `en-US`, then the default value, and
finally falls back to the key itself. Keys that start with `unity:` are handed to `L10n.Tr`, so Unity translates its
own strings into the editor language.

## What is in the package

| Piece | Purpose |
| --- | --- |
| `BasicLocalization` | One asset per language: the localization id, the display name shown in the picker, and the translations |
| `BasicTranslate` | One entry: `key`, `translate`, an optional tooltip and its suffix |
| `LocalizationPackage` | The assembly attribute that connects an editor assembly to an id |
| `LocalizationHelper` / `S()` / `UILocalize()` | Lookups, live updates and the language picker |
| Format conversion | Import and export the assets as JSON or PO, merging by key |

The selected language is stored per id in `EditorPrefs`, so each package remembers its own choice.

## Documentation

The documentation site walks through the assets, the inspector and the runtime API:

- [Overview](https://t2penbix99wcoxkv3a4g.github.io/VPM-Docs/#/localization/Overview) - how the pieces fit together
- [Installation](https://t2penbix99wcoxkv3a4g.github.io/VPM-Docs/#/localization/Installation) - assets, translations and the JSON/PO round trip
- [Inspector](https://t2penbix99wcoxkv3a4g.github.io/VPM-Docs/#/localization/Inspector) - editing the translation list
- [Unity keys](https://t2penbix99wcoxkv3a4g.github.io/VPM-Docs/#/localization/Unity-Keys) - translating Unity's own strings
- [API](https://t2penbix99wcoxkv3a4g.github.io/VPM-Docs/#/localization/API) - every public type and member
- [Troubleshooting](https://t2penbix99wcoxkv3a4g.github.io/VPM-Docs/#/localization/Troubleshooting) - keys that stay untranslated

The site is available in English, Japanese, Simplified Chinese and Traditional Chinese; the links above open the
English text and the language picker in the corner switches the rest.

## License

MIT, see [LICENSE](https://github.com/T2PeNBiX99wcoxKv3A4g/VPM.Localization/blob/main/LICENSE).
