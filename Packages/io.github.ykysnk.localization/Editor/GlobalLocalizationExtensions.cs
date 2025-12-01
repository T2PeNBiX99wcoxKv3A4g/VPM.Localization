using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace io.github.ykysnk.Localization.Editor;

[PublicAPI]
public static class GlobalLocalizationExtensions
{
    public static string L(this string str, string localizationID) => GlobalLocalization.L(localizationID, str);

    public static GUIContent G(this string str, string localizationID, string tooltip) =>
        GlobalLocalization.G(localizationID, str, tooltip);

    public static GUIContent G(this string str, string localizationID) =>
        GlobalLocalization.G(localizationID, str, str + GlobalLocalization.TooltipExt);

    public static GUIContent G(this SerializedProperty property, string localizationID) =>
        GlobalLocalization.G(localizationID, property);
}