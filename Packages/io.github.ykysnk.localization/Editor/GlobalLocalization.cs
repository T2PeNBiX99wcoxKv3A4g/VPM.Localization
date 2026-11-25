using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using io.github.ykysnk.utils;
using io.github.ykysnk.utils.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.ykysnk.Localization.Editor
{
    [InitializeOnLoad]
    public static class GlobalLocalization
    {
        public delegate void LocalizationChanged(string localizationID, string newLanguage);

        public delegate void LocalizationUpdated();

        public const string TooltipExt = ":tooltip";
        public const string LanguageLabelKey = "label.language";
        public const string DefaultLangKey = "en-US";
        public const string DefaultLocalization = "Default";
        public const string Null = "--null--";

        public static LocalizationHelper DefaultHelper = new(DefaultLocalization);

        private static Dictionary<string, string[]> _languageKeyList = new();
        private static Dictionary<string, string[]> _languageKeyNames = new();

        private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>>
            LanguageDictionary = new();

        private static readonly Dictionary<string, GUIContent> GuiContents = new();

        private static bool _isDelayLoaded;

        private static bool _loadQueued;

        static GlobalLocalization() => EditorApplication.delayCall += DelayLoad;

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

        public static Dictionary<string, string> GetLanguageLocalization(string localizationID, string language)
        {
            if (!LanguageDictionary.TryGetValue(localizationID, out var localizationContents))
                throw new ArgumentException($"Localization ID {localizationID} not found!", nameof(localizationID));
            return !localizationContents.TryGetValue(language, out var contents)
                ? throw new ArgumentException($"Language {language} not found for localization ID {localizationID}!",
                    nameof(language))
                : contents.ToDictionary(x => x.Key, x => x.Value);
        }

        public static string S(string localizationID, string? key, string? defaultValue = null)
        {
            var theKey = key ?? Null;

            if (!LanguageDictionary.TryGetValue(localizationID, out var localizationContents))
                return defaultValue ?? theKey;

            var englishContents = localizationContents.GetValueOrDefault(DefaultLangKey, new());
            var contents = localizationContents.GetValueOrDefault(GetSelectedLanguage(localizationID), new());
            var get = contents.GetValueOrDefault(theKey,
                englishContents.GetValueOrDefault(theKey, defaultValue ?? theKey));
            return string.IsNullOrEmpty(get) ? englishContents.GetValueOrDefault(theKey, defaultValue ?? theKey) : get;
        }

        public static bool S(string localizationID, string? key, out string? localizationString)
        {
            var theKey = key ?? Null;
            localizationString = null;

            if (!LanguageDictionary.TryGetValue(localizationID, out var localizationContents))
                return false;

            var englishContents = localizationContents.GetValueOrDefault(DefaultLangKey, new());
            var contents = localizationContents.GetValueOrDefault(GetSelectedLanguage(localizationID), new());

            return contents.TryGetValue(theKey, out localizationString) ||
                   englishContents.TryGetValue(theKey, out localizationString);
        }

        public static GUIContent G(string localizationID, string key, Texture? image, string? tooltip)
        {
            var guiKey = $"{localizationID}.{key}";

            if (!GuiContents.TryGetValue(guiKey, out var content))
                return GuiContents[guiKey] = new(S(localizationID, key), image, S(localizationID, tooltip, ""));

            content.text = S(localizationID, key);
            content.image = image;
            content.tooltip = S(localizationID, tooltip, "");
            return content;
        }

        public static void SelectLanguageGUI(string localizationID)
        {
            var keyList = _languageKeyList[localizationID];
            var keyNames = _languageKeyNames[localizationID];

            var index = Array.IndexOf(keyList, GetSelectedLanguage(localizationID));
            if (index < 0) index = 0;

            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUILayout.Popup(G(localizationID, LanguageLabelKey, null, LanguageLabelKey + TooltipExt),
                index, keyNames);
            if (EditorGUI.EndChangeCheck())
                SetSelectedLanguage(localizationID, keyList[newIndex]);
        }

        public static void SelectLanguageElement(string localizationID, VisualElement element, int spaceHeight = 8)
        {
            if (!TryGetLanguageLists(localizationID, out var languageIds, out var displayNames))
            {
                // The dictionary is not built yet (or the id is unknown); retry after the next editor tick.
                ScheduleLoad();
                return;
            }

            var selectedLanguage = LanguageDictionary.ContainsKey(localizationID)
                ? GetSelectedLanguage(localizationID)
                : DefaultLangKey;

            var selectedIndex = Array.IndexOf(languageIds, selectedLanguage);
            if (selectedIndex < 0)
            {
                // The pref points at a language that was renamed or removed. Reload once (the asset may be new),
                // then fall back to the first language and repair the pref without firing a change event.
                Load();
                if (!TryGetLanguageLists(localizationID, out languageIds, out displayNames)) return;

                selectedIndex = Array.IndexOf(languageIds, selectedLanguage);
                if (selectedIndex < 0)
                {
                    selectedIndex = 0;
                    EditorPrefs.SetString($"{localizationID}_Language", languageIds[selectedIndex]);
                }
            }

            // The popup carries indices instead of names, so two languages with the same display name stay distinct
            // and the formatter callbacks stay free of side effects.
            var choices = Enumerable.Range(0, languageIds.Length).ToList();
            var languagePopup = new PopupField<int>(S(localizationID, LanguageLabelKey), choices, selectedIndex,
                index => displayNames[index], index => displayNames[index])
            {
                tooltip = S(localizationID, LanguageLabelKey + TooltipExt)
            };

            languagePopup.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue < 0 || evt.newValue >= languageIds.Length) return;
                SetSelectedLanguage(localizationID, languageIds[evt.newValue]);
            });

            var space = new VisualElement
            {
                style =
                {
                    height = spaceHeight
                }
            };

            element.Add(space);
            element.Add(languagePopup);
            UpdateHelper.Register(localizationID, LanguageLabelKey, (label, tooltip) =>
            {
                languagePopup.label = label;
                languagePopup.tooltip = tooltip;
            });
        }

        private static bool TryGetLanguageLists(string localizationID, out string[] languageIds,
            out string[] displayNames)
        {
            if (_languageKeyList.TryGetValue(localizationID, out languageIds!) &&
                _languageKeyNames.TryGetValue(localizationID, out displayNames!) &&
                languageIds.Length == displayNames.Length)
                return true;

            languageIds = Array.Empty<string>();
            displayNames = Array.Empty<string>();
            return false;
        }

        public static string NameToLocalizationName(string name)
        {
            var builder = new StringBuilder(name.Length + 8);

            for (var i = 0; i < name.Length; i++)
            {
                var nameChar = name[i];

                if (char.IsUpper(nameChar))
                {
                    var previous = i > 0 ? name[i - 1] : '\0';
                    var next = i + 1 < name.Length ? name[i + 1] : '\0';

                    // Start a new word at a lower-to-upper boundary, or before the last capital of an acronym run
                    // ("UIText" -> "ui_text", "HPBar" -> "hp_bar", "localizationID" -> "localization_id").
                    if (i > 0 && (!char.IsUpper(previous) || char.IsLower(next)))
                        builder.Append('_');

                    nameChar = char.ToLowerInvariant(nameChar);
                }

                if (char.IsSymbol(nameChar) || char.IsPunctuation(nameChar))
                    nameChar = '_';

                builder.Append(nameChar);
            }

            return builder.ToString();
        }

        private static void OnLocalizationFileUpdated(string localizationID, string localizationName) => ScheduleLoad();

        private static void OnLocalizationError(Exception exception, BasicEditor.Type type)
        {
            if (type == BasicEditor.Type.UGUI) return;

            // A UI Toolkit editor failed; the localization data may be stale. Reload after the current editor tick
            // instead of inside the error handler.
            ScheduleLoad();
        }

        // Coalesces the reload requests that OnValidate and error handlers produce, so editing a field does not scan
        // every localization asset per keystroke.
        private static void ScheduleLoad()
        {
            if (_loadQueued) return;
            _loadQueued = true;
            EditorApplication.delayCall += FlushLoad;
        }

        private static void FlushLoad()
        {
            _loadQueued = false;
            Load();
        }

        private static void DelayLoad()
        {
            if (_isDelayLoaded) return;
            _isDelayLoaded = true;
            Load();
        }

        [MenuItem("Tools/Localization/Reload Localizations", false, 1000)]
        private static void Load()
        {
            BasicLocalization.OnLocalizationFileUpdated -= OnLocalizationFileUpdated;
            BasicEditor.OnErrorEvent -= OnLocalizationError;
            EditorApplication.delayCall -= DelayLoad;
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

            Utils.Log(nameof(GlobalLocalization),
                $"Found {basicLocalizations.Length} {nameof(BasicLocalization)} assets!");

            foreach (var basicLocalization in basicLocalizations)
            {
                if (basicLocalization == null) continue;

                var localizationID = basicLocalization.localizationID;

                if (string.IsNullOrEmpty(localizationID))
                {
                    Utils.LogWarning(nameof(GlobalLocalization),
                        $"Localization ID is empty in {AssetDatabase.GetAssetPath(basicLocalization)}!");
                    continue;
                }

                LanguageDictionary.TryAdd(localizationID, new());
                LanguageDictionary[localizationID].TryAdd(basicLocalization.name, new());

                foreach (var basicTranslate in basicLocalization.translates)
                {
                    if (string.IsNullOrEmpty(basicTranslate.key))
                    {
                        Utils.LogWarning(nameof(GlobalLocalization),
                            $"Key is empty in {AssetDatabase.GetAssetPath(basicLocalization)}!");
                        continue;
                    }

                    LanguageDictionary[localizationID][basicLocalization.name]
                        .TryAdd(basicTranslate.key, basicTranslate.translate);

                    if (!basicTranslate.useTooltip) continue;

                    var suffix = string.IsNullOrEmpty(basicTranslate.tooltipSuffix)
                        ? ".tooltip"
                        : basicTranslate.tooltipSuffix;

                    LanguageDictionary[localizationID][basicLocalization.name]
                        .TryAdd(basicTranslate.key + suffix, basicTranslate.tooltip);

                    // Keep the historical ":tooltip" spelling resolvable, so Tooltip(key) works for entries that use
                    // another suffix such as ".tooltip".
                    if (suffix != TooltipExt)
                        LanguageDictionary[localizationID][basicLocalization.name]
                            .TryAdd(basicTranslate.key + TooltipExt, basicTranslate.tooltip);
                }

                langKeyList.TryAdd(localizationID, new());
                langKeyList[localizationID].Add(basicLocalization.name);

                langDisplayNames.TryAdd(localizationID, new());
                langDisplayNames[localizationID].TryAdd(basicLocalization.name, basicLocalization.displayName);
            }

            var languageDisplayNames = langDisplayNames.ToDictionary(x => x.Key,
                x => x.Value.OrderBy(y => y.Value).ToDictionary(y => y.Key, y => y.Value));
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
            BasicLocalization.OnLocalizationFileUpdated += OnLocalizationFileUpdated;
            BasicEditor.OnErrorEvent += OnLocalizationError;
            EditorApplication.delayCall += DelayLoad;
        }
    }
}