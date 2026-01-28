using System;
using System.Collections.Generic;
using System.Linq;
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
                return GuiContents[guiKey] = new(S(localizationID, key), image, S(localizationID, tooltip));

            content.text = S(localizationID, key);
            content.image = image;
            content.tooltip = S(localizationID, tooltip);
            return content;
        }

        public static void SelectLanguageGUI(string localizationID)
        {
            var keyList = _languageKeyList[localizationID];
            var keyNames = _languageKeyNames[localizationID];

            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUILayout.Popup(G(localizationID, LanguageLabelKey, null, LanguageLabelKey + TooltipExt),
                Array.IndexOf(keyList, GetSelectedLanguage(localizationID)),
                keyNames);
            if (EditorGUI.EndChangeCheck())
                SetSelectedLanguage(localizationID, keyList[newIndex]);
        }

        public static void SelectLanguageElement(string localizationID, VisualElement element, int spaceHeight = 8)
        {
            var keyList = _languageKeyList[localizationID].ToList();
            var keyNames = _languageKeyNames[localizationID].ToList();
            var selectedLanguage = GetSelectedLanguage(localizationID);

            if (!keyList.Contains(selectedLanguage))
                Load();

            var defaultIndex = keyList.IndexOf(selectedLanguage);
            var languagePopup = new PopupField<string>(S(localizationID, LanguageLabelKey), keyNames,
                keyNames[defaultIndex], s =>
                {
                    SetSelectedLanguage(localizationID, keyList[keyNames.IndexOf(s)]);
                    return s;
                }, s => s)
            {
                tooltip = S(localizationID, LanguageLabelKey + TooltipExt)
            };

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

        public static string NameToLocalizationName(string name)
        {
            var returnString = "";
            var lastUpperIndex = -1;

            for (var i = 0; i < name.Length; i++)
            {
                var classNameChar = name[i];

                if (char.IsUpper(classNameChar))
                {
                    if (i > 0 && i - 1 != lastUpperIndex) returnString += "_";
                    classNameChar = char.ToLowerInvariant(classNameChar);
                    lastUpperIndex = i;
                }

                if (char.IsSymbol(classNameChar) || char.IsPunctuation(classNameChar))
                    classNameChar = '_';

                returnString += classNameChar;
            }

            return returnString;
        }

        private static void OnLocalizationFileUpdated(string localizationID, string localizationName) => Load();

        private static void OnLocalizationError(Exception exception, BasicEditor.Type type)
        {
            if (type == BasicEditor.Type.UGUI) return;
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