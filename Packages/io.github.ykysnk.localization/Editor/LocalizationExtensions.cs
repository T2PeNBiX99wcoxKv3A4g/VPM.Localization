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
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string S(this string key) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).S(key);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string S(this SerializedProperty property) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).S(property);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Sf(this string key, params object?[] args) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).Sf(key, args);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static GUIContent G(this string key) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).G(key);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static GUIContent G(this SerializedProperty property) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).G(property);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Tooltip(this string key) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).Tooltip(key);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Tooltip(this SerializedProperty property) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).Tooltip(property);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UILocalize(this VisualElement element, bool addLanguagePopup = true,
            int spaceHeight = 8) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).UILocalize(element, addLanguagePopup, spaceHeight);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static UpdateHelper Register(this SerializedProperty property, UpdateHelper.Callback callback) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).UpdateRegister(property, callback);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static UpdateHelper Register(this string localizeKey, UpdateHelper.Callback callback) =>
            LocalizationHelper.For(Assembly.GetCallingAssembly()).UpdateRegister(localizeKey, callback);
    }
}
