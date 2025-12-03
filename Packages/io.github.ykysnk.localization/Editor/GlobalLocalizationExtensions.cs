using System;
using JetBrains.Annotations;
using UnityEngine;

namespace io.github.ykysnk.Localization.Editor;

[PublicAPI]
[Obsolete("Use LocalizationHelper instead.")]
public static class GlobalLocalizationExtensions
{
    [Obsolete("Use LocalizationHelper.S(key) instead.")]
    public static string L(this string key, string localizationID) => GlobalLocalization.S(localizationID, key);

    [Obsolete("Use LocalizationHelper.G(key) instead.")]
    public static GUIContent G(this string key, string localizationID) =>
        GlobalLocalization.G(localizationID, key, null, key + GlobalLocalization.TooltipExt);
}