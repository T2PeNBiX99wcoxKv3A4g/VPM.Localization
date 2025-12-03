using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using io.github.ykysnk.utils;
using UnityEditor;
using UnityEngine;

namespace io.github.ykysnk.Localization.Editor;

[InitializeOnLoad]
public static class GlobalLocalization
{
    public delegate void LocalizationChanged(string localizationID, string newLanguage);

    public delegate void LocalizationUpdated();

    public const string TooltipExt = ".tooltip";
    public const string LanguageLabelKey = "label.language";
    public const string DefaultLangKey = "en-US";
    public const string DefaultLocalization = "Default";
    public const string Null = "--null--";

    private static Dictionary<string, string[]> _languageKeyList = new();
    private static Dictionary<string, string[]> _languageKeyNames = new();

    private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>>
        LanguageDictionary = new();

    private static readonly Dictionary<string, GUIContent> GuiContents = new();

    static GlobalLocalization() => Load();

    public static event LocalizationUpdated? OnLocalizationReload;
    public static event LocalizationChanged? OnLocalizationChanged;

    public static string GetSelectedLanguage(string localizationID) => !LanguageDictionary.ContainsKey(localizationID)
        ? throw new ArgumentException($"Localization ID {localizationID} not found!", nameof(localizationID))
        : EditorPrefs.GetString($"{localizationID}_Language", DefaultLangKey);

    public static void SetSelectedLanguage(string localizationID, string language)
    {
        if (!LanguageDictionary.ContainsKey(localizationID))
            throw new ArgumentException($"Localization ID {localizationID} not found!", nameof(localizationID));
        if (!_languageKeyList[localizationID].Contains(language))
            throw new ArgumentException($"Language {language} not found for localization ID {localizationID}!",
                nameof(language));
        EditorPrefs.SetString($"{localizationID}_Language", language);
        OnLocalizationChanged?.Invoke(localizationID, language);
    }

    public static string L(string localizationID, string? key)
    {
        var theKey = key ?? Null;

        if (!LanguageDictionary.TryGetValue(localizationID, out var contents))
            return theKey;

        if (!contents.TryGetValue(GetSelectedLanguage(localizationID), out var languageContents))
            return contents.TryGetValue(DefaultLangKey, out var languageContents2)
                ? languageContents2.GetValueOrDefault(theKey, theKey)
                : theKey;

        var englishContents = contents.GetValueOrDefault(DefaultLangKey, new());
        return languageContents.GetValueOrDefault(theKey, englishContents.GetValueOrDefault(theKey, theKey));
    }

    public static GUIContent G(string localizationID, string key) => G(localizationID, key, null, "");

    public static GUIContent G(string localizationID, string[] key) => key.Length == 2
        ? G(localizationID, key[0], null, key[1])
        : G(localizationID, key[0], null, null);

    public static GUIContent G(string localizationID, string key, string tooltip) =>
        G(localizationID, key, null, tooltip); // From EditorToolboxSettings

    public static GUIContent G(string localizationID, string key, Texture? image) => G(localizationID, key, image, "");

    public static GUIContent G(string localizationID, SerializedProperty property) =>
        G(localizationID, property.name, $"{property.name}{TooltipExt}");

    public static GUIContent G(string localizationID, string key, Texture? image, string? tooltip)
    {
        var guiKey = $"{localizationID}.{key}";

        if (!GuiContents.TryGetValue(guiKey, out var content))
            return GuiContents[guiKey] = new(L(localizationID, key), image, L(localizationID, tooltip));

        content.text = L(localizationID, key);
        content.image = image;
        content.tooltip = L(localizationID, tooltip);
        return content;
    }

    public static void SelectLanguageGUI(string localizationID)
    {
        var keyList = _languageKeyList[localizationID];
        var keyNames = _languageKeyNames[localizationID];

        EditorGUI.BeginChangeCheck();
        var newIndex = EditorGUILayout.Popup(G(localizationID, LanguageLabelKey, LanguageLabelKey + TooltipExt),
            Array.IndexOf(keyList, GetSelectedLanguage(localizationID)),
            keyNames);
        if (EditorGUI.EndChangeCheck())
            SetSelectedLanguage(localizationID, keyList[newIndex]);
    }

    [MenuItem("Tools/Localization/Reload Languages")]
    private static void Load()
    {
        BasicLocalization.OnLanguageUpdated -= Load;
        LanguageDictionary.Clear();
        GuiContents.Clear();

        var langDisplayNames = new Dictionary<string, Dictionary<string, string>>();
        var langKeyList = new Dictionary<string, List<string>>();

        // Refs: https://discussions.unity.com/t/how-can-i-find-all-instances-of-a-scriptable-object-in-the-project-editor/198002/3
        var basicLocalizations = AssetDatabase.FindAssets($"t:{nameof(BasicLocalization)}").Select(x =>
        {
            var path = AssetDatabase.GUIDToAssetPath(x);
            return AssetDatabase.LoadAssetAtPath<BasicLocalization>(path);
        }).ToArray();

        Utils.Log(nameof(GlobalLocalization), $"Found {basicLocalizations.Length} {nameof(BasicLocalization)} assets!");

        foreach (var basicLocalization in basicLocalizations)
        {
            var localizationID = basicLocalization.localizationID;

            LanguageDictionary.TryAdd(localizationID, new());
            LanguageDictionary[localizationID].TryAdd(basicLocalization.name, new());

            foreach (var basicTranslate in basicLocalization.translates)
            {
                if (string.IsNullOrEmpty(basicTranslate.key))
                {
                    Utils.LogWarning(nameof(GlobalLocalization),
                        $"Key is empty for localization {localizationID}.{basicLocalization.name}!");
                    continue;
                }

                LanguageDictionary[localizationID][basicLocalization.name]
                    .TryAdd(basicTranslate.key, basicTranslate.translate);

                LanguageDictionary[localizationID][basicLocalization.name]
                    .TryAdd(basicTranslate.key + TooltipExt, basicTranslate.tooltip);
            }

            langKeyList.TryAdd(localizationID, new());
            langKeyList[localizationID].Add(basicLocalization.name);

            langDisplayNames.TryAdd(localizationID, new());
            langDisplayNames[localizationID].TryAdd(basicLocalization.name, basicLocalization.displayName);
        }

        var languageDisplayNames = langDisplayNames.ToDictionary(x => x.Key,
            x => x.Value.ToImmutableSortedDictionary().WithComparers(StringComparer.OrdinalIgnoreCase));
        langDisplayNames.Clear();

        _languageKeyList = langKeyList.ToDictionary(x => x.Key, x => x.Value.ToArray());

        var tempLanguageKeyNames = new Dictionary<string, string[]>();

        foreach (var (id, sortedDictionary) in languageDisplayNames)
        {
            var languageIDList = _languageKeyList[id];
            var length = languageIDList.Length;
            var names = new string[length];

            for (var i = 0; i < length; i++)
                names[i] = sortedDictionary[languageIDList[i]];

            tempLanguageKeyNames.TryAdd(id, names);
        }

        _languageKeyNames = tempLanguageKeyNames;

        langKeyList.Clear();
        OnLocalizationReload?.Invoke();
        BasicLocalization.OnLanguageUpdated += Load;
    }
}