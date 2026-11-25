using System;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.ykysnk.Localization.Editor
{
    internal static class InternalLocalizationExtensions
    {
        internal static string S(this string key) => GlobalLocalization.Helper.S(key);
        internal static string S(this SerializedProperty property) => GlobalLocalization.Helper.S(property);

        internal static string Sf(this string key, params object?[] args) =>
            GlobalLocalization.Helper.Sf(key, args);

        internal static GUIContent G(this string key) => GlobalLocalization.Helper.G(key);
        internal static GUIContent G(this SerializedProperty property) => GlobalLocalization.Helper.G(property);

        internal static string Tooltip(this string key) => GlobalLocalization.Helper.Tooltip(key);

        internal static string Tooltip(this SerializedProperty property) =>
            GlobalLocalization.Helper.Tooltip(property);

        internal static void UILocalize(this VisualElement element, bool addLanguagePopup = true,
            int spaceHeight = 8) =>
            GlobalLocalization.Helper.UILocalize(element, addLanguagePopup, spaceHeight);

        internal static UpdateHelper Register(this SerializedProperty property, UpdateHelper.Callback callback) =>
            GlobalLocalization.Helper.UpdateRegister(property, callback);

        internal static UpdateHelper Register(this string localizeKey, UpdateHelper.Callback callback) =>
            GlobalLocalization.Helper.UpdateRegister(localizeKey, callback);

        /// <summary>
        ///     Tries to read a <see cref="BasicTranslate" /> from the clipboard. The text must parse as JSON and carry a
        ///     key and a translation, so an empty clipboard or an unrelated JSON object is rejected instead of
        ///     overwriting the row with empty values.
        /// </summary>
        /// <param name="basicTranslate">The parsed entry when the method returns true; otherwise null.</param>
        /// <param name="exception">The parse or validation error, or null when the clipboard was simply empty.</param>
        /// <returns>True when a usable entry was read.</returns>
        internal static bool TryPasteTranslate(
            [NotNullWhen(true)] out BasicTranslate? basicTranslate,
            out Exception? exception)
        {
            basicTranslate = null;
            exception = null;

            var clipboard = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(clipboard)) return false;

            try
            {
                basicTranslate = JsonUtility.FromJson<BasicTranslate>(clipboard);
            }
            catch (Exception e)
            {
                exception = e;
                return false;
            }

            if (basicTranslate == null)
            {
                exception = new FormatException("Clipboard does not contain a translation entry.");
                return false;
            }

            if (string.IsNullOrEmpty(basicTranslate.key) || basicTranslate.translate == null)
            {
                basicTranslate = null;
                exception = new FormatException(
                    "Clipboard does not contain a translation: the JSON needs a non-empty 'key' and a 'translate' value.");
                return false;
            }

            return true;
        }
    }
}