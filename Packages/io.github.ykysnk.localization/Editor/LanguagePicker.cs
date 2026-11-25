using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace io.github.ykysnk.Localization.Editor
{
    /// <summary>
    ///     Ready-made language picker for UI Toolkit: it lists every language of one localization id and switches the
    ///     selected one on change, which updates every element that was localized through <c>UILocalize</c>.
    ///     <para>
    ///         A plain <see cref="VisualElement" />, so it can be dropped into any tree and styled through the
    ///         <c>language-picker</c> class. Create one with <see cref="LocalizationHelper.CreateLanguagePicker" />;
    ///         for a fully custom control, read <see cref="LocalizationHelper.Languages" /> and call
    ///         <see cref="LocalizationHelper.SetLanguage" /> instead.
    ///     </para>
    /// </summary>
    [PublicAPI]
    public class LanguagePicker : VisualElement
    {
        private readonly string _localizationID;
        private UpdateHelper? _labelRegistration;
        private string[] _languageIds = Array.Empty<string>();
        private PopupField<int>? _popup;
        private GlobalLocalization.LocalizationUpdated? _reloadHandler;

        /// <summary>Creates the picker for one localization id. Prefer <see cref="LocalizationHelper.CreateLanguagePicker" />.</summary>
        public LanguagePicker(string localizationID)
        {
            _localizationID = localizationID;
            AddToClassList("language-picker");

            // The language assets are loaded on a delay; build now when they are already there.
            GlobalLocalization.EnsureLoaded();
            Rebuild();

            _reloadHandler = Rebuild;
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                GlobalLocalization.OnLocalizationReload += _reloadHandler;
                GlobalLocalization.OnLocalizationChanged += OnLanguageChanged;
                Rebuild();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                GlobalLocalization.OnLocalizationReload -= _reloadHandler;
                GlobalLocalization.OnLocalizationChanged -= OnLanguageChanged;
                _labelRegistration?.Dispose();
                _labelRegistration = null;
            });
        }

        /// <summary>Language id that is selected now, for example <c>ja-JP</c>.</summary>
        public string SelectedLanguage
        {
            get => GlobalLocalization.GetSelectedLanguage(_localizationID);
            set => GlobalLocalization.SetSelectedLanguage(_localizationID, value);
        }

        /// <summary>
        ///     Rebuilds the popup. An element that was created before the delayed first load gets its entries here,
        ///     through <see cref="GlobalLocalization.OnLocalizationReload" />.
        /// </summary>
        private void Rebuild()
        {
            if (!GlobalLocalization.TryGetLanguageLists(_localizationID, out var ids, out var names) || ids.Length == 0)
                return;

            _languageIds = ids;
            _labelRegistration?.Dispose();
            _labelRegistration = null;
            Clear();

            var selectedIndex = Array.IndexOf(ids, GlobalLocalization.GetSelectedLanguage(_localizationID));
            if (selectedIndex < 0)
            {
                // The pref points at a language that was renamed or removed; fall back to the first one.
                selectedIndex = 0;
                GlobalLocalization.SetSelectedLanguage(_localizationID, ids[0]);
            }

            var choices = Enumerable.Range(0, ids.Length).ToList();
            var popup = new PopupField<int>(Text(GlobalLocalization.LanguageLabelKey), choices, selectedIndex,
                index => names[index], index => names[index])
            {
                tooltip = Text(GlobalLocalization.LanguageLabelKey + GlobalLocalization.TooltipExt)
            };

            popup.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue < 0 || evt.newValue >= _languageIds.Length) return;
                GlobalLocalization.SetSelectedLanguage(_localizationID, _languageIds[evt.newValue]);
            });

            _labelRegistration = UpdateHelper.Register(_localizationID, GlobalLocalization.LanguageLabelKey,
                (label, tooltip) =>
                {
                    popup.label = label;
                    popup.tooltip = tooltip;
                });

            _popup = popup;
            Add(popup);
        }

        /// <summary>Keeps the control in sync when the language is switched from somewhere else.</summary>
        private void OnLanguageChanged(string localizationID, string language)
        {
            if (localizationID != _localizationID || _popup == null) return;

            var index = Array.IndexOf(_languageIds, language);
            if (index >= 0) _popup.SetValueWithoutNotify(index);
        }

        private string Text(string key) => GlobalLocalization.S(_localizationID, key);
    }
}