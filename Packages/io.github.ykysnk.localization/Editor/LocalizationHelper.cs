using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.ykysnk.Localization.Editor
{
    [PublicAPI]
    public class LocalizationHelper
    {
        public delegate void LocalizationChanged(string newLanguage);

        public delegate void LocalizationUpdated();

        private static readonly List<string> SkipName = new()
        {
            "unity-text-input",
            "unity-content"
        };

        private static readonly List<string> SkipClass = new()
        {
            "unity-base-field__inspector-field"
        };

        private readonly string _localizationID;

        public LocalizationHelper(string localizationID)
        {
            _localizationID = localizationID;
            GlobalLocalization.OnLocalizationChanged += OnLocalizationChangedInternal;
        }

        public string SelectedLanguage
        {
            get => GlobalLocalization.GetSelectedLanguage(_localizationID);
            set => GlobalLocalization.SetSelectedLanguage(_localizationID, value);
        }

        public event LocalizationChanged? OnLocalizationChanged;

        private void OnLocalizationChangedInternal(string localizationID, string newLanguage)
        {
            if (localizationID != _localizationID) return;
            OnLocalizationChanged?.Invoke(newLanguage);
        }

        public void SelectLanguageGUI() => GlobalLocalization.SelectLanguageGUI(_localizationID);

        public void SelectLanguageElement(VisualElement element, int spaceHeight = 8) =>
            GlobalLocalization.SelectLanguageElement(_localizationID, element, spaceHeight);

        public void UpdateRegister(string localizeKey, UpdateHelper.Callback callback) =>
            UpdateHelper.Register(_localizationID, localizeKey, callback);

        public void UpdateRegister(SerializedProperty property, UpdateHelper.Callback callback) =>
            UpdateHelper.Register(_localizationID, property, callback);

        public Dictionary<string, string> GetLanguageLocalization(string language) =>
            GlobalLocalization.GetLanguageLocalization(_localizationID, language);

        public string S(string key, string? defaultValue = null) =>
            GlobalLocalization.S(_localizationID, key, defaultValue);

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

        public GUIContent G(string key) => G(key, null);
        public GUIContent G(string key, Texture? image) => G(key, image, key + GlobalLocalization.TooltipExt);

        public GUIContent G(string key, Texture? image, string? tooltip) =>
            GlobalLocalization.G(_localizationID, key, image, tooltip);

        public string S(SerializedProperty property) =>
            S(
                $"label.{GlobalLocalization.NameToLocalizationName(property.serializedObject.targetObject.GetType().Name)}.{GlobalLocalization.NameToLocalizationName(property.name)}");

        public GUIContent G(SerializedProperty property) =>
            G(
                $"label.{GlobalLocalization.NameToLocalizationName(property.serializedObject.targetObject.GetType().Name)}.{GlobalLocalization.NameToLocalizationName(property.name)}");

        public string Tooltip(string key, string? defaultValue = null) => S(key + GlobalLocalization.TooltipExt);
        public string TooltipF(string key, params object?[] args) => Sf(key + GlobalLocalization.TooltipExt, args);

        public string Tooltip(SerializedProperty property) => S(
            $"label.{GlobalLocalization.NameToLocalizationName(property.serializedObject.targetObject.GetType().Name)}.{GlobalLocalization.NameToLocalizationName(property.name)}{GlobalLocalization.TooltipExt}",
            "");

        // Refs: nadena.dev.modular_avatar.core.editor.UIElementLocalizer
        public void UILocalize(VisualElement elem, bool addLanguagePopup = true, int spaceHeight = 8)
        {
            WalkTree(elem);
            if (!addLanguagePopup) return;
            SelectLanguageElement(elem, spaceHeight);
        }

        private void WalkTree(VisualElement elem)
        {
            var clemType = elem.GetType();

            // Without the delay call, the bind will not be set correctly.
            EditorApplication.delayCall += () => TryLocalize(elem, clemType);

            foreach (var child in elem.Children())
            {
                if (SkipName.Contains(child.name) || child.ClassListContains("localize-skip")) continue;
                WalkTree(child);
            }
        }

        private void TryLocalize(VisualElement elem, Type type)
        {
            if (SkipClass.Any(elem.ClassListContains)) return;
            var textProperty = type.GetProperty("text");
            var labelProperty = type.GetProperty("label");
            var headerTitleProperty = type.GetProperty("headerTitle");
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

            if (keyProperty == null || key == null) return;

            keyProperty.SetValue(elem, S(key));
            elem.tooltip = Tooltip(key);

#if LOCALIZATION_TEST
            Utils.Log(nameof(UILocalize),
                $"Localizing {elem.GetType().FullName}, {elem.name}, {string.Join(", ", elem.GetClasses())}, {key}, {keyProperty}");
#endif

            UpdateRegister(key, (label, tooltip) =>
            {
                keyProperty?.SetValue(elem, label);
                elem.tooltip = tooltip;
            });
        }
    }
}