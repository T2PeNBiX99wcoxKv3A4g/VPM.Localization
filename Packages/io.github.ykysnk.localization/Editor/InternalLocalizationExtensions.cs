using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace io.github.ykysnk.Localization.Editor;

[PublicAPI]
internal static class InternalLocalizationExtensions
{
    internal static string S(this string key) => GlobalLocalization.DefaultHelper.S(key);
    internal static string S(this SerializedProperty property) => GlobalLocalization.DefaultHelper.S(property);
    internal static string Sf(this string key, params object?[] args) => GlobalLocalization.DefaultHelper.Sf(key, args);

    internal static GUIContent G(this string key) => GlobalLocalization.DefaultHelper.G(key);
    internal static GUIContent G(this SerializedProperty property) => GlobalLocalization.DefaultHelper.G(property);

    internal static string Tooltip(this string key) => GlobalLocalization.DefaultHelper.Tooltip(key);

    internal static string Tooltip(this SerializedProperty property) =>
        GlobalLocalization.DefaultHelper.Tooltip(property);

    internal static void Register(this SerializedProperty property, UpdateHelper.Callback callback) =>
        GlobalLocalization.DefaultHelper.UpdateRegister(property, callback);

    internal static void Register(this string localizeKey, UpdateHelper.Callback callback) =>
        GlobalLocalization.DefaultHelper.UpdateRegister(localizeKey, callback);
}