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
}