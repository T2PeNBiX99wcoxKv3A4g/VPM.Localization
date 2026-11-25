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
        /// <summary>Receives the localization that changed and the language that is selected now.</summary>
        public delegate void LocalizationChanged(string localizationID, string newLanguage);

        /// <summary>Raised after the localization assets were loaded again.</summary>
        public delegate void LocalizationUpdated();

        /// <summary>Suffix that marks a key as the tooltip of another key.</summary>
        internal const string TooltipExt = BasicTranslate.DefaultTooltipSuffix;

        internal const string LanguageLabelKey = "label.language";
        internal const string DefaultLangKey = "en-US";
        internal const string Null = "--null--";

        /// <summary>Prefix that marks a key as a Unity editor string, translated through <c>L10n.Tr</c>.</summary>
        internal const string UnityPrefix = "unity:";

        private static Dictionary<string, string[]> _languageKeyList = new();
        private static Dictionary<string, string[]> _languageKeyNames = new();

        private static readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>>
            LanguageDictionary = new();

        private static readonly Dictionary<string, GUIContent> GuiContents = new();

        // Every live UpdateHelper. The helpers do not subscribe to the events themselves: with a subscription per
        // registration, a window with a thousand localized elements spent most of its build time combining delegates.
        private static readonly HashSet<UpdateHelper> ActiveHelpers = new();

        // Returned instead of allocating an empty dictionary for a language that has no asset yet.
        private static readonly Dictionary<string, string> EmptyContents = new();

        // How long the reload waits after the last request, so typing in a translation asset does not rescan the
        // whole project on every keystroke.
        private const double ReloadDelaySeconds = 0.25;

        private static bool _isDelayLoaded;
        private static bool _reloadQueued;
        private static double _reloadAt;

        static GlobalLocalization() => EditorApplication.delayCall += DelayLoad;

        /// <summary>Raised after the assets were (re)loaded, so every label can be refreshed.</summary>
        public static event LocalizationUpdated? OnLocalizationReload;

        /// <summary>Raised when the selected language of one localization changes.</summary>
        public static event LocalizationChanged? OnLocalizationChanged;

        internal static string GetSelectedLanguage(string localizationID) => !LanguageDictionary.ContainsKey(localizationID)
            ? throw new ArgumentException($"Localization ID {localizationID} not found!", nameof(localizationID))
            : EditorPrefs.GetString($"{localizationID}_Language", DefaultLangKey);

        /// <summary>Whether the assets of this localization were loaded, so a missing key can be told from a not-yet-loaded one.</summary>
        internal static bool HasLocalization(string localizationID) => LanguageDictionary.ContainsKey(localizationID);

        internal static void SetSelectedLanguage(string localizationID, string language)
        {
            if (!LanguageDictionary.ContainsKey(localizationID))
                throw new ArgumentException($"Localization ID {localizationID} not found!", nameof(localizationID));
            if (!_languageKeyList[localizationID].Contains(language))
                throw new ArgumentException($"Language {language} not found for localization ID {localizationID}!",
                    nameof(language));
            EditorPrefs.SetString($"{localizationID}_Language", language);
            NotifyHelpers(localizationID);
            OnLocalizationChanged?.Invoke(localizationID, language);
        }

        /// <summary>Registers a helper that wants to be told when the data or the selected language changes.</summary>
        internal static void RegisterHelper(UpdateHelper helper) => ActiveHelpers.Add(helper);

        internal static void UnregisterHelper(UpdateHelper helper) => ActiveHelpers.Remove(helper);

        /// <summary>
        ///     Refreshes every helper, or only the ones of one localization when <paramref name="localizationID" /> is
        ///     given. Snapshot first: a callback may add or remove helpers while it runs.
        /// </summary>
        private static void NotifyHelpers(string? localizationID)
        {
            if (ActiveHelpers.Count == 0) return;

            foreach (var helper in ActiveHelpers.ToArray())
            {
                if (localizationID != null && helper.LocalizationID != localizationID) continue;
                helper.Refresh();
            }
        }

        internal static Dictionary<string, string> GetLanguageLocalization(string localizationID, string language)
        {
            if (!LanguageDictionary.TryGetValue(localizationID, out var localizationContents))
                throw new ArgumentException($"Localization ID {localizationID} not found!", nameof(localizationID));
            return !localizationContents.TryGetValue(language, out var contents)
                ? throw new ArgumentException($"Language {language} not found for localization ID {localizationID}!",
                    nameof(language))
                : contents.ToDictionary(x => x.Key, x => x.Value);
        }

        internal static string S(string localizationID, string? key, string? defaultValue = null)
        {
            var theKey = key ?? Null;

            if (!LanguageDictionary.TryGetValue(localizationID, out var localizationContents))
                return defaultValue ?? theKey;

            // TryGetValue instead of GetValueOrDefault(key, new()): the default argument would allocate an empty
            // dictionary on every lookup, and this runs for every localized element.
            var englishContents = localizationContents.TryGetValue(DefaultLangKey, out var english)
                ? english
                : EmptyContents;
            var contents = localizationContents.TryGetValue(GetSelectedLanguage(localizationID), out var selected)
                ? selected
                : EmptyContents;
            var get = contents.GetValueOrDefault(theKey,
                englishContents.GetValueOrDefault(theKey, defaultValue ?? theKey));
            return string.IsNullOrEmpty(get) ? englishContents.GetValueOrDefault(theKey, defaultValue ?? theKey) : get;
        }

        internal static bool S(string localizationID, string? key, out string? localizationString)
        {
            var theKey = key ?? Null;
            localizationString = null;

            if (!LanguageDictionary.TryGetValue(localizationID, out var localizationContents))
                return false;

            var englishContents = localizationContents.TryGetValue(DefaultLangKey, out var english)
                ? english
                : EmptyContents;
            var contents = localizationContents.TryGetValue(GetSelectedLanguage(localizationID), out var selected)
                ? selected
                : EmptyContents;

            return contents.TryGetValue(theKey, out localizationString) ||
                   englishContents.TryGetValue(theKey, out localizationString);
        }

        internal static GUIContent G(string localizationID, string key, Texture? image, string? tooltip)
        {
            var guiKey = $"{localizationID}.{key}";

            if (!GuiContents.TryGetValue(guiKey, out var content))
                return GuiContents[guiKey] = new(S(localizationID, key), image, S(localizationID, tooltip, ""));

            content.text = S(localizationID, key);
            content.image = image;
            content.tooltip = S(localizationID, tooltip, "");
            return content;
        }

        internal static void SelectLanguageGUI(string localizationID)
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

        internal static void SelectLanguageElement(string localizationID, VisualElement element, int spaceHeight = 8)
        {
            if (!TryGetLanguageLists(localizationID, out var languageIds, out var displayNames))
            {
                // The first inspector after a domain reload is built before the delayed load has run. Load now, because
                // this method creates the popup only once, while the tree is built.
                EnsureLoaded();

                if (!TryGetLanguageLists(localizationID, out languageIds, out displayNames))
                {
                    // Still nothing for this id; retry after the next editor tick.
                    ScheduleLoad();
                    return;
                }
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

            var registration = UpdateHelper.Register(localizationID, LanguageLabelKey, (label, tooltip) =>
            {
                languagePopup.label = label;
                languagePopup.tooltip = tooltip;
            });

            // Without this the helper stays in UpdateHelper's static list (and keeps the popup and its window alive)
            // for every inspector that was ever built.
            languagePopup.RegisterCallback<DetachFromPanelEvent>(_ => registration.Dispose());
        }

        internal static bool TryGetLanguageLists(string localizationID, out string[] languageIds,
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

        /// <summary>Language ids of a localization in display order. Empty until the assets are loaded.</summary>
        internal static IReadOnlyList<string> GetLanguageIds(string localizationID) =>
            _languageKeyList.TryGetValue(localizationID, out var ids) ? ids : Array.Empty<string>();

        /// <summary>Display names matching <see cref="GetLanguageIds" />. Empty until the assets are loaded.</summary>
        internal static IReadOnlyList<string> GetLanguageNames(string localizationID) =>
            _languageKeyNames.TryGetValue(localizationID, out var names) ? names : Array.Empty<string>();

        /// <summary>
        ///     A <c>unity:</c> entry is the English text that <c>L10n.Tr</c> translates, so it has to be declared in the
        ///     English asset. An entry that only exists in another language stops resolving as soon as that language is
        ///     not selected, and one without English text leaves the raw key in the UI.
        /// </summary>
        private static void WarnAboutUnityKeys(List<(string ID, string Key, string Path)> entries)
        {
            foreach (var (localizationID, key, path) in entries.Distinct())
            {
                if (!LanguageDictionary.TryGetValue(localizationID, out var languages)) continue;

                if (!languages.TryGetValue(DefaultLangKey, out var english))
                {
                    Utils.LogWarning(nameof(GlobalLocalization),
                        $"There is no {DefaultLangKey} asset for {localizationID}, so {key} declared in {path} " +
                        "cannot be translated by Unity.");
                    continue;
                }

                if (!english.ContainsKey(key))
                    Utils.LogWarning(nameof(GlobalLocalization),
                        $"{key} is declared in {path} but missing from the {DefaultLangKey} asset of " +
                        $"{localizationID}; L10n.Tr() needs the English text, so the label shows the raw key.");
                else if (string.IsNullOrEmpty(english[key]))
                    Utils.LogWarning(nameof(GlobalLocalization),
                        $"{key} has an empty translation in the {DefaultLangKey} asset of {localizationID}.");
            }
        }

        internal static string NameToLocalizationName(string name)
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
            _reloadAt = EditorApplication.timeSinceStartup + ReloadDelaySeconds;

            if (_reloadQueued) return;
            _reloadQueued = true;
            EditorApplication.update += WaitForReload;
        }

        private static void WaitForReload()
        {
            if (EditorApplication.timeSinceStartup < _reloadAt) return;

            EditorApplication.update -= WaitForReload;
            _reloadQueued = false;
            Load();
        }

        private static void DelayLoad() => EnsureLoaded();

        /// <summary>
        ///     Loads the assets now if the delayed load has not run yet. An inspector that survives a domain reload asks
        ///     for this while its tree is being built, which happens before <see cref="DelayLoad" /> gets its turn.
        /// </summary>
        internal static void EnsureLoaded()
        {
            if (_isDelayLoaded) return;
            _isDelayLoaded = true;
            Load();
        }

        [MenuItem("Tools/Localization/Reload Localizations", false, 1000)]
        private static void Load()
        {
            if (_reloadQueued)
            {
                EditorApplication.update -= WaitForReload;
                _reloadQueued = false;
            }

            BasicLocalization.OnLocalizationFileUpdated -= OnLocalizationFileUpdated;
            BasicEditor.OnErrorEvent -= OnLocalizationError;
            EditorApplication.delayCall -= DelayLoad;
            LanguageDictionary.Clear();
            GuiContents.Clear();

            var langDisplayNames = new Dictionary<string, Dictionary<string, string>>();
            var langKeyList = new Dictionary<string, List<string>>();
            var unityKeys = new List<(string ID, string Key, string Path)>();

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

                    if (basicTranslate.key.StartsWith(UnityPrefix, StringComparison.Ordinal))
                        unityKeys.Add((localizationID, basicTranslate.key,
                            AssetDatabase.GetAssetPath(basicLocalization)));

                    if (!basicTranslate.useTooltip) continue;

                    var suffix = string.IsNullOrEmpty(basicTranslate.tooltipSuffix)
                        ? TooltipExt
                        : basicTranslate.tooltipSuffix;

                    LanguageDictionary[localizationID][basicLocalization.name]
                        .TryAdd(basicTranslate.key + suffix, basicTranslate.tooltip);

                    // Keep ":tooltip" resolvable for entries that override the suffix, so Tooltip(key) works for
                    // every entry no matter which spelling its own suffix uses.
                    if (suffix != TooltipExt)
                        LanguageDictionary[localizationID][basicLocalization.name]
                            .TryAdd(basicTranslate.key + TooltipExt, basicTranslate.tooltip);
                }

                langKeyList.TryAdd(localizationID, new());
                langKeyList[localizationID].Add(basicLocalization.name);

                langDisplayNames.TryAdd(localizationID, new());
                langDisplayNames[localizationID].TryAdd(basicLocalization.name, basicLocalization.displayName);
            }

            // The language popup shows the display names, so the languages are ordered by them. The comparer is explicit
            // because the default one depends on the machine's culture.
            _languageKeyList = langKeyList.ToDictionary(
                x => x.Key,
                x => x.Value
                    .OrderBy(language => langDisplayNames[x.Key][language], StringComparer.InvariantCulture)
                    .ToArray());

            _languageKeyNames = _languageKeyList.ToDictionary(
                x => x.Key,
                x => x.Value.Select(language => langDisplayNames[x.Key][language]).ToArray());
            WarnAboutUnityKeys(unityKeys);
            NotifyHelpers(null);
            OnLocalizationReload?.Invoke();
            BasicLocalization.OnLocalizationFileUpdated += OnLocalizationFileUpdated;
            BasicEditor.OnErrorEvent += OnLocalizationError;
            EditorApplication.delayCall += DelayLoad;
        }
    }
}