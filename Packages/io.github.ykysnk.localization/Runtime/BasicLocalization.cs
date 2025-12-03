using JetBrains.Annotations;
using UnityEngine;

namespace io.github.ykysnk.Localization
{
    [PublicAPI]
    [CreateAssetMenu(fileName = "BasicLocalization", menuName = "Localization/Basic Localization")]
    public class BasicLocalization : ScriptableObject
    {
        public delegate void LocalizationFileUpdated(string localizationID, string localizationName);

        private const string DefaultDisplayName = "English";

        public string localizationID = "";
        public string displayName = DefaultDisplayName;

        public BasicTranslate[] translates =
        {
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