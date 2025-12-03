using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace io.github.ykysnk.Localization.Editor;

[PublicAPI]
internal static class InternalLocalizationExtensions
{
    internal static string S(this string key) => GlobalLocalization.DefaultHelper.S(key);
    internal static GUIContent G(this string key) => GlobalLocalization.DefaultHelper.G(key);
    internal static GUIContent G(this SerializedProperty property) => GlobalLocalization.DefaultHelper.G(property);
    internal static string S(this SerializedProperty property) => GlobalLocalization.DefaultHelper.S(property);

    internal static void Register(this SerializedProperty property, UpdateHelper.UpdateLabel callback) =>
        GlobalLocalization.DefaultHelper.UpdateRegister(property, callback);

    internal static void Register(this string localizeKey, UpdateHelper.UpdateLabel callback) =>
        GlobalLocalization.DefaultHelper.UpdateRegister(localizeKey, callback);
}