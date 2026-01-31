using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace io.github.ykysnk.Localization
{
    [PublicAPI]
    [CreateAssetMenu(fileName = "en-US", menuName = "Localization/Basic Localization")]
    public class BasicLocalization : ScriptableObject
    {
        public delegate void LocalizationFileUpdated(string localizationID, string localizationName);

        private const string DefaultDisplayName = "English";

        public string localizationID = "";
        public string displayName = DefaultDisplayName;

        public List<BasicTranslate> translates = new()
        {
            new()
            {
                key = "label.language",
                translate = "Language",
                tooltip = "Change your target language in the editor."
            }
        };

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(displayName))
                displayName = DefaultDisplayName;
            OnLocalizationFileUpdated?.Invoke(localizationID, name);
        }

        public static event LocalizationFileUpdated? OnLocalizationFileUpdated;
    }
}