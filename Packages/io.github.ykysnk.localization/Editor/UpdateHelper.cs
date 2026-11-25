using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;

namespace io.github.ykysnk.Localization.Editor
{
    /// <summary>
    ///     Keeps one label/tooltip pair up to date when the localization is reloaded or the language changes.
    ///     Every registration is independent, so several elements can share the same key, and
    ///     <see cref="Dispose" /> removes it again when the element goes away.
    /// </summary>
    [PublicAPI]
    public class UpdateHelper : IDisposable
    {
        public delegate void Callback(string newLabel, string newTooltip);

        private static readonly Dictionary<string, List<UpdateHelper>> UpdateHelpers = new();

        private readonly Callback _callback;
        private readonly string _localizationID;
        private readonly string _localizeKey;

        private UpdateHelper(string localizationID, string localizeKey, Callback callback)
        {
            _localizationID = localizationID;
            _localizeKey = localizeKey;
            _callback = callback;
            GlobalLocalization.OnLocalizationReload += OnLocalizationReload;
            GlobalLocalization.OnLocalizationChanged += OnLocalizationChanged;
        }

        private string FullKey => $"{_localizationID}.{_localizeKey}";

        /// <summary>Stops updating the callback. Call this when the element it updates is removed.</summary>
        public void Dispose()
        {
            GlobalLocalization.OnLocalizationReload -= OnLocalizationReload;
            GlobalLocalization.OnLocalizationChanged -= OnLocalizationChanged;

            if (!UpdateHelpers.TryGetValue(FullKey, out var helpers)) return;

            helpers.Remove(this);
            if (helpers.Count == 0)
                UpdateHelpers.Remove(FullKey);
        }

        /// <summary>Registers a callback for one localization key.</summary>
        /// <param name="localizationID">Localization group the key belongs to.</param>
        /// <param name="localizeKey">Key whose label and <c>:tooltip</c> entry are reported.</param>
        /// <param name="callback">Invoked with the label and tooltip whenever the data or language changes.</param>
        /// <returns>A handle that unregisters the callback when disposed.</returns>
        public static UpdateHelper Register(string localizationID, string localizeKey, Callback callback)
        {
            var helper = new UpdateHelper(localizationID, localizeKey, callback);

            if (!UpdateHelpers.TryGetValue(helper.FullKey, out var helpers))
            {
                helpers = new();
                UpdateHelpers.Add(helper.FullKey, helpers);
            }

            helpers.Add(helper);
            return helper;
        }

        /// <summary>Registers the key that a label property would use, derived from its type and field name.</summary>
        public static UpdateHelper Register(string localizationID, SerializedProperty property, Callback callback) =>
            Register(localizationID,
                $"label.{GlobalLocalization.NameToLocalizationName(property.serializedObject.targetObject.GetType().Name)}.{GlobalLocalization.NameToLocalizationName(property.name)}",
                callback);

        private void OnLocalizationReload()
        {
            _callback(GlobalLocalization.S(_localizationID, _localizeKey),
                GlobalLocalization.S(_localizationID, _localizeKey + GlobalLocalization.TooltipExt, ""));
        }

        private void OnLocalizationChanged(string id, string newLanguage)
        {
            if (id != _localizationID) return;
            OnLocalizationReload();
        }
    }
}