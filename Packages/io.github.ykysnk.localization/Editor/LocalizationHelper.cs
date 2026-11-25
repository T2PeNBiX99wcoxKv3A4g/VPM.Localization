using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using io.github.ykysnk.utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.ykysnk.Localization.Editor
{
    [PublicAPI]
    public class LocalizationHelper
    {
        /// <summary>Receives the language that was just selected.</summary>
        public delegate void LocalizationChanged(string newLanguage);

        /// <summary>Raised when the localization assets were loaded again.</summary>
        public delegate void LocalizationUpdated();

        private static readonly HashSet<string> WarnedUnityKeys = new();

        private static readonly List<string> SkipName = new()
        {
            "unity-text-input",
            "unity-content"
        };

        private static readonly List<string> SkipClass = new()
        {
            "unity-base-field__inspector-field",
            "unity-object-field-display__label"
        };

        private static readonly Dictionary<Type, LocalizableProperties> PropertyCache = new();

        private readonly struct LocalizableProperties
        {
            internal readonly PropertyInfo? Text;
            internal readonly PropertyInfo? Label;
            internal readonly PropertyInfo? HeaderTitle;

            internal LocalizableProperties(Type type)
            {
                Text = type.GetProperty("text");
                Label = type.GetProperty("label");
                HeaderTitle = type.GetProperty("headerTitle");
            }
        }

        private static readonly Dictionary<Assembly, LocalizationHelper> AssemblyHelpers = new();

        // An assembly without the attribute keeps showing its keys, which is what a missing translation does anyway.
        private static readonly LocalizationHelper Unbound = new("");

        private readonly string _localizationID;

        private LocalizationChanged? _localizationChanged;
        private bool _subscribed;

        /// <summary>
        ///     Creates a helper for one localization id. Prefer <see cref="For(Assembly)" /> or <see cref="For(Type)" />,
        ///     which read the id from the assembly's <see cref="LocalizationPackageAttribute" />.
        /// </summary>
        public LocalizationHelper(string localizationID) => _localizationID = localizationID;

        /// <summary>
        ///     Subscribes to <see cref="GlobalLocalization.OnLocalizationChanged" /> only while something listens here: a
        ///     helper created for a one-off lookup used to keep that static subscription (and itself) alive forever.
        /// </summary>
        public event LocalizationChanged? OnLocalizationChanged
        {
            add
            {
                _localizationChanged += value;
                Subscribe();
            }
            remove
            {
                _localizationChanged -= value;
                if (_localizationChanged == null) Unsubscribe();
            }
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;
            GlobalLocalization.OnLocalizationChanged += OnLocalizationChangedInternal;
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _subscribed = false;
            GlobalLocalization.OnLocalizationChanged -= OnLocalizationChangedInternal;
        }

        /// <summary>
        ///     Language id that is selected now, for example <c>ja-JP</c>. Setting it switches the language and updates
        ///     every element that was localized through <see cref="UILocalize" /> or <see cref="UpdateRegister" />.
        /// </summary>
        public string SelectedLanguage
        {
            get => GlobalLocalization.GetSelectedLanguage(_localizationID);
            set => GlobalLocalization.SetSelectedLanguage(_localizationID, value);
        }

        /// <summary>
        ///     Switches the language of this localization and updates every element that was localized through
        ///     <see cref="UILocalize" /> or <see cref="UpdateRegister" />.
        /// </summary>
        public void SetLanguage(string language) => SelectedLanguage = language;

        /// <summary>Language ids of this localization in display order. Empty until the assets are loaded.</summary>
        public IReadOnlyList<string> Languages => GlobalLocalization.GetLanguageIds(_localizationID);

        /// <summary>Display names matching <see cref="Languages" />.</summary>
        public IReadOnlyList<string> LanguageNames => GlobalLocalization.GetLanguageNames(_localizationID);

        /// <summary>A ready-made UI Toolkit language picker for this localization.</summary>
        public LanguagePicker CreateLanguagePicker() => new(_localizationID);

        /// <summary>Helper declared by <paramref name="assembly" />'s <see cref="LocalizationPackageAttribute" />.</summary>
        public static LocalizationHelper For(Assembly assembly)
        {
            if (AssemblyHelpers.TryGetValue(assembly, out var helper)) return helper;

            var localizationID = assembly.GetCustomAttribute<LocalizationPackageAttribute>()?.LocalizationID;

            if (localizationID == null)
            {
                Utils.LogWarning(nameof(LocalizationHelper),
                    $"{assembly.GetName().Name} has no [assembly: LocalizationPackage(\"...\")], so its keys are shown as they are.");
                return AssemblyHelpers[assembly] = Unbound;
            }

            return AssemblyHelpers[assembly] = new(localizationID);
        }

        /// <summary>Helper declared by the assembly that contains <paramref name="type" />.</summary>
        public static LocalizationHelper For(Type type) => For(type.Assembly);

        private void OnLocalizationChangedInternal(string localizationID, string newLanguage)
        {
            if (localizationID != _localizationID) return;
            _localizationChanged?.Invoke(newLanguage);
        }

        /// <summary>Draws the IMGUI language picker (an <c>EditorGUILayout.Popup</c>) for this localization.</summary>
        public void SelectLanguageGUI() => GlobalLocalization.SelectLanguageGUI(_localizationID);

        /// <summary>
        ///     Appends a UI Toolkit language picker to <paramref name="element" />. Prefer
        ///     <see cref="CreateLanguagePicker" /> when the control has to be placed or styled.
        /// </summary>
        /// <param name="element">Container the picker is added to.</param>
        /// <param name="spaceHeight">Height of the gap inserted before the picker.</param>
        public void SelectLanguageElement(VisualElement element, int spaceHeight = 8) =>
            GlobalLocalization.SelectLanguageElement(_localizationID, element, spaceHeight);

        /// <summary>
        ///     Calls <paramref name="callback" /> now and again whenever the language changes or the assets are
        ///     reloaded. Dispose the handle when the element it updates goes away.
        /// </summary>
        /// <param name="localizeKey">Key whose label and <c>:tooltip</c> entry are reported.</param>
        /// <param name="callback">Receives the new label and tooltip.</param>
        public UpdateHelper UpdateRegister(string localizeKey, UpdateHelper.Callback callback) =>
            UpdateHelper.Register(_localizationID, localizeKey, callback);

        /// <summary>
        ///     Same as <see cref="UpdateRegister(string, UpdateHelper.Callback)" /> for the key derived from
        ///     <paramref name="property" /> (its target type and field name).
        /// </summary>
        public UpdateHelper UpdateRegister(SerializedProperty property, UpdateHelper.Callback callback) =>
            UpdateHelper.Register(_localizationID, property, callback);

        /// <summary>Snapshot of every key and text pair of one language.</summary>
        /// <param name="language">Language id, for example <c>en-US</c>.</param>
        public Dictionary<string, string> GetLanguageLocalization(string language) =>
            GlobalLocalization.GetLanguageLocalization(_localizationID, language);

        /// <summary>
        ///     Text of <paramref name="key" /> in the selected language. Falls back to English, then to
        ///     <paramref name="defaultValue" />, then to the key itself, so a missing translation never throws.
        /// </summary>
        public string S(string key, string? defaultValue = null) =>
            GlobalLocalization.S(_localizationID, key, defaultValue);

        /// <summary>
        ///     Looks up <paramref name="key" /> without falling back to the key itself; returns false when it has no
        ///     text in any language.
        /// </summary>
        public bool S(string key, out string? localizationString) =>
            GlobalLocalization.S(_localizationID, key, out localizationString);

        /// <summary>
        ///     <see cref="S(string, string?)" /> of <paramref name="key" /> run through <see cref="string.Format" />.
        ///     A format string that does not match the arguments is returned with the arguments appended instead of
        ///     throwing.
        /// </summary>
        public string Sf(string key, params object?[] args)
        {
            var get = S(key);

            try
            {
                return string.Format(get, args);
            }
            catch (FormatException)
            {
                return get + "(" + string.Join(", ", args) + ")"; // from modular avatar
            }
        }

        /// <summary>GUIContent with the text of <paramref name="key" /> and its tooltip.</summary>
        public GUIContent G(string key) => G(key, null);

        /// <summary>GUIContent with the text of <paramref name="key" />, an image and its tooltip.</summary>
        public GUIContent G(string key, Texture? image) => G(key, image, key + GlobalLocalization.TooltipExt);

        /// <summary>
        ///     GUIContent with the text of <paramref name="key" />, an image and the tooltip stored under
        ///     <paramref name="tooltip" />.
        /// </summary>
        public GUIContent G(string key, Texture? image, string? tooltip) =>
            GlobalLocalization.G(_localizationID, key, image, tooltip);

        /// <summary>Text for the key derived from <paramref name="property" />: <c>label.{Type}.{field}</c>.</summary>
        public string S(SerializedProperty property) =>
            S(
                $"label.{GlobalLocalization.NameToLocalizationName(property.serializedObject.targetObject.GetType().Name)}.{GlobalLocalization.NameToLocalizationName(property.name)}");

        /// <summary>GUIContent for the key derived from <paramref name="property" />.</summary>
        public GUIContent G(SerializedProperty property) =>
            G(
                $"label.{GlobalLocalization.NameToLocalizationName(property.serializedObject.targetObject.GetType().Name)}.{GlobalLocalization.NameToLocalizationName(property.name)}");

        /// <summary>Tooltip of <paramref name="key" />, that is the entry stored with the <c>:tooltip</c> suffix.</summary>
        public string Tooltip(string key, string? defaultValue = null) =>
            S(key + GlobalLocalization.TooltipExt, defaultValue);

        /// <summary>Looks up the tooltip of <paramref name="key" /> without a fallback.</summary>
        public bool Tooltip(string key, out string? tooltip) => S(key + GlobalLocalization.TooltipExt, out tooltip);

        /// <summary>Tooltip for the key derived from <paramref name="property" />.</summary>
        public string Tooltip(SerializedProperty property) => S(
            $"label.{GlobalLocalization.NameToLocalizationName(property.serializedObject.targetObject.GetType().Name)}.{GlobalLocalization.NameToLocalizationName(property.name)}{GlobalLocalization.TooltipExt}",
            "");

        /// <summary>
        ///     Replaces the keys in <paramref name="elem" />'s tree with their translations and keeps them up to date:
        ///     <c>text</c>, <c>label</c> and <c>headerTitle</c> attributes, plus tooltips. Call it before binding the
        ///     tree; the second, delayed pass repairs values that the binding put back.
        /// </summary>
        /// <param name="elem">Root of the tree to localize.</param>
        /// <param name="addLanguagePopup">Also append the language picker to <paramref name="elem" />.</param>
        /// <param name="spaceHeight">Height of the gap before the appended picker.</param>
        // Refs: nadena.dev.modular_avatar.core.editor.UIElementLocalizer
        public void UILocalize(VisualElement elem, bool addLanguagePopup = true, int spaceHeight = 8)
        {
            // The language assets are loaded on a delay as well; a tree built before that would show the raw keys.
            GlobalLocalization.EnsureLoaded();

            // Localize right away, so the keys never reach the screen, and once more after the binding pass: the
            // bindings run after UILocalize and can put the key back. The first pass registers the elements, the
            // second only rewrites the text of the ones that were overwritten.
            LocalizeTree(elem, true);
            elem.schedule.Execute(() => LocalizeTree(elem, false));

            if (!addLanguagePopup) return;
            SelectLanguageElement(elem, spaceHeight);
        }

        private void LocalizeTree(VisualElement elem, bool register)
        {
            TryLocalize(elem, elem.GetType(), register);

            foreach (var child in elem.Children())
            {
                if (SkipName.Contains(child.name) || child.ClassListContains("localize-skip")) continue;
                LocalizeTree(child, register);
            }
        }

        private void TryLocalize(VisualElement elem, Type type, bool register)
        {
            foreach (var skipped in SkipClass)
                if (elem.ClassListContains(skipped))
                    return;

            // Reflection per element is what made a large window crawl; the properties only depend on the type.
            if (!PropertyCache.TryGetValue(type, out var properties))
                PropertyCache[type] = properties = new(type);

            var textProperty = properties.Text;
            var labelProperty = properties.Label;
            var headerTitleProperty = properties.HeaderTitle;
            string? key = null;
            PropertyInfo? keyProperty = null;

            if (textProperty != null && textProperty.SetMethod != null)
            {
                key = textProperty.GetValue(elem) as string;
                keyProperty = textProperty;
            }
            else if (labelProperty != null && labelProperty.SetMethod != null)
            {
                key = labelProperty.GetValue(elem) as string;
                keyProperty = labelProperty;
            }
            else if (headerTitleProperty != null && headerTitleProperty.SetMethod != null)
            {
                key = headerTitleProperty.GetValue(elem) as string;
                keyProperty = headerTitleProperty;
            }

            // For dumb null check
            if (keyProperty != null && key != null && !string.IsNullOrWhiteSpace(key))
            {
                if (key.StartsWith(GlobalLocalization.UnityPrefix, StringComparison.Ordinal))
                {
                    var source = S(key, out var localized) ? localized! : key;

                    if (localized == null && GlobalLocalization.HasLocalization(_localizationID) &&
                        WarnedUnityKeys.Add(key))
                        Utils.LogWarning(nameof(LocalizationHelper),
                            $"'{key}' has no entry in the localization assets, so the label shows the raw key. " +
                            $"Add the English text of the Unity string to the {GlobalLocalization.DefaultLangKey} " +
                            "asset; L10n.Tr() translates that text.");

                    keyProperty.SetValue(elem, L10n.Tr(source));
                    if (!elem.ClassListContains("localize-tooltip"))
                        elem.tooltip = L10n.Tr(Tooltip(key, elem.tooltip));
                }
                else
                {
                    keyProperty.SetValue(elem, S(key));
                    if (!elem.ClassListContains("localize-tooltip"))
                        elem.tooltip = Tooltip(key, elem.tooltip);

#if LOCALIZATION_TEST
                    Utils.Log(nameof(UILocalize),
                        $"Localizing {elem.GetType().FullName}, {elem.name}, {string.Join(", ", elem.GetClasses())}, {key}, {keyProperty}");
#endif

                    if (!register) return;

                    var registration = UpdateRegister(key, (label, tooltip) =>
                    {
                        keyProperty?.SetValue(elem, label);
                        if (!elem.ClassListContains("localize-tooltip"))
                            elem.tooltip = tooltip;
                    });
                    elem.RegisterCallback<DetachFromPanelEvent>(_ => registration.Dispose());
                }
            }

            if (!register) return;
            if (!elem.ClassListContains("localize-tooltip")) return;

            var tooltipKey = elem.tooltip;
            if (tooltipKey.StartsWith(GlobalLocalization.UnityPrefix, StringComparison.Ordinal))
            {
                elem.tooltip = L10n.Tr(S(tooltipKey));
                return;
            }

            elem.tooltip = S(tooltipKey);
            var tooltipRegistration = UpdateRegister(tooltipKey, (_, tooltip) => elem.tooltip = tooltip);
            elem.RegisterCallback<DetachFromPanelEvent>(_ => tooltipRegistration.Dispose());
        }
    }
}