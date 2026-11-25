using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace io.github.ykysnk.Localization.Editor
{
    /// <summary>
    ///     The editor UI sugar: <c>"label.some_key".S()</c>, <c>tree.UILocalize()</c> and friends, with the helper taken
    ///     from the <see cref="LocalizationPackageAttribute" /> of the assembly that makes the call. A package only has
    ///     to declare that attribute once, so it does not need a wrapper class of its own.
    /// </summary>
    /// <remarks>
    ///     Every method carries <see cref="MethodImplOptions.NoInlining" />: <see cref="Assembly.GetCallingAssembly" />
    ///     reports this assembly instead of the consumer as soon as the call gets inlined into it.
    /// </remarks>
    [PublicAPI]
    public static class LocalizationExtensions
    {
        /// <summary>Text of <paramref name="key" /> in the calling assembly's localization.</summary>
        /// <param name="key">Key to look up, for example <c>label.my_window.title</c>.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string S(this string key) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).S(key);

        /// <summary>Text for the key derived from <paramref name="property" /> (<c>label.{Type}.{field}</c>).</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string S(this SerializedProperty property) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).S(property);

        /// <summary>Text of <paramref name="key" /> run through <see cref="string.Format" />.</summary>
        /// <param name="key">Key to look up.</param>
        /// <param name="args">Arguments for the format string.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Sf(this string key, params object?[] args) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).Sf(key, args);

        /// <summary>GUIContent with the text of <paramref name="key" /> and its tooltip.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static GUIContent G(this string key) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).G(key);

        /// <summary>GUIContent for the key derived from <paramref name="property" />.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static GUIContent G(this SerializedProperty property) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).G(property);

        /// <summary>Tooltip of <paramref name="key" /> (the entry stored with the <c>:tooltip</c> suffix).</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Tooltip(this string key) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).Tooltip(key);

        /// <summary>Tooltip for the key derived from <paramref name="property" />.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Tooltip(this SerializedProperty property) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).Tooltip(property);

        /// <summary>
        ///     Replaces the keys in <paramref name="element" />'s tree with their translations and keeps them up to date.
        ///     Call it before binding the tree.
        /// </summary>
        /// <param name="element">Root of the tree to localize.</param>
        /// <param name="addLanguagePopup">Also append the language picker to <paramref name="element" />.</param>
        /// <param name="spaceHeight">Height of the gap before the appended picker.</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UILocalize(this VisualElement element, bool addLanguagePopup = true,
            int spaceHeight = 8) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).UILocalize(element, addLanguagePopup, spaceHeight);

        /// <summary>
        ///     Registers <paramref name="callback" /> for the key derived from <paramref name="property" />. Dispose the
        ///     handle when the element it updates goes away.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static UpdateHelper Register(this SerializedProperty property, UpdateHelper.Callback callback) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).UpdateRegister(property, callback);

        /// <summary>
        ///     Registers <paramref name="callback" /> for <paramref name="localizeKey" />. Dispose the handle when the
        ///     element it updates goes away.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static UpdateHelper Register(this string localizeKey, UpdateHelper.Callback callback) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).UpdateRegister(localizeKey, callback);
    }
}
