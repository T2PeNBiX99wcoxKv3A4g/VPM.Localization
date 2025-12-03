using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.ykysnk.Localization.Editor;

[PublicAPI]
public class LocalizationHelper
{
    public delegate void LocalizationChanged(string newLanguage);

    public delegate void LocalizationUpdated();

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

    public void SelectLanguageElement(VisualElement element) =>
        GlobalLocalization.SelectLanguageElement(_localizationID, element);

    public void UpdateRegister(string localizeKey, UpdateHelper.Callback callback) =>
        UpdateHelper.Register(_localizationID, localizeKey, callback);

    public void UpdateRegister(SerializedProperty property, UpdateHelper.Callback callback) =>
        UpdateHelper.Register(_localizationID, property, callback);

    public string S(string key, string? defaultValue = null) => GlobalLocalization.S(_localizationID, key, defaultValue);

    public string Sf(string key, params object[] args)
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
}