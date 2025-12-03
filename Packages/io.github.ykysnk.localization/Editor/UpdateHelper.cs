using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;

namespace io.github.ykysnk.Localization.Editor;

[PublicAPI]
public class UpdateHelper
{
    public delegate void UpdateLabel(string newLabel);

    private static readonly Dictionary<string, UpdateHelper> UpdateHelpers = new();
    private readonly UpdateLabel _callback;
    private readonly string _localizationID;
    private readonly string _localizeKey;

    private UpdateHelper(string localizationID, string localizeKey, UpdateLabel callback)
    {
        _localizationID = localizationID;
        _localizeKey = localizeKey;
        _callback = callback;
        GlobalLocalization.OnLocalizationReload += OnLocalizationReload;
        GlobalLocalization.OnLocalizationChanged += OnLocalizationChanged;
    }

    public static void Register(string localizationID, string localizeKey, UpdateLabel callback)
    {
        var fullKey = $"{localizationID}.{localizeKey}";
        UpdateHelpers.Remove(fullKey);
        var helper = new UpdateHelper(localizationID, localizeKey, callback);
        UpdateHelpers.Add(fullKey, helper);
    }

    public static void Register(string localizationID, SerializedProperty property, UpdateLabel callback) =>
        Register(localizationID,
            $"label.{GlobalLocalization.NameToLocalizationName(property.serializedObject.targetObject.GetType().Name)}.{GlobalLocalization.NameToLocalizationName(property.name)}",
            callback);

    private void EventUnregister()
    {
        GlobalLocalization.OnLocalizationReload -= OnLocalizationReload;
        GlobalLocalization.OnLocalizationChanged -= OnLocalizationChanged;
    }

    private void OnLocalizationReload()
    {
        _callback(GlobalLocalization.S(_localizationID, _localizeKey));
    }

    private void OnLocalizationChanged(string id, string newLanguage)
    {
        if (id != _localizationID) return;
        OnLocalizationReload();
    }
}