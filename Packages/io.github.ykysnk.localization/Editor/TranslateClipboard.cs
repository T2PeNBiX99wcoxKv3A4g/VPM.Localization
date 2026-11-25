using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace io.github.ykysnk.Localization.Editor
{
    /// <summary>Clipboard handling for translation entries, shared by the inspector and the row drawer.</summary>
    [PublicAPI]
    public static class TranslateClipboard
    {
        private static string? _lastClipboard;
        private static bool _lastHasEntry;

        /// <summary>
        ///     Whether the clipboard currently holds a translation entry. The answer is cached for the clipboard text,
        ///     so a button can ask for it often without parsing the JSON again.
        /// </summary>
        public static bool HasEntry()
        {
            var clipboard = EditorGUIUtility.systemCopyBuffer;
            if (clipboard == _lastClipboard) return _lastHasEntry;

            _lastClipboard = clipboard;
            _lastHasEntry = TryRead(out _, out _);
            return _lastHasEntry;
        }

        /// <summary>
        ///     Tries to read a <see cref="BasicTranslate" /> from the clipboard. The text must parse as JSON and carry a
        ///     key, so an empty clipboard or an unrelated JSON object is rejected instead of overwriting the row with
        ///     empty values.
        /// </summary>
        /// <param name="basicTranslate">The parsed entry when the method returns true; otherwise null.</param>
        /// <param name="exception">The parse or validation error, or null when the clipboard was simply empty.</param>
        /// <returns>True when a usable entry was read.</returns>
        public static bool TryRead([NotNullWhen(true)] out BasicTranslate? basicTranslate, out Exception? exception)
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

            // JsonUtility runs the field initializers, so 'translate' is "" when the JSON leaves it out; only the key
            // tells a translation entry apart from any other JSON object.
            if (string.IsNullOrEmpty(basicTranslate.key))
            {
                basicTranslate = null;
                exception = new FormatException(
                    "Clipboard does not contain a translation: the JSON needs a non-empty 'key'.");
                return false;
            }

            return true;
        }
    }
}