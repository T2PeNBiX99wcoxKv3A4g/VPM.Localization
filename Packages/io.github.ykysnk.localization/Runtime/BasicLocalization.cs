using JetBrains.Annotations;
using UnityEngine;

namespace io.github.ykysnk.Localization
{
    [PublicAPI]
    [CreateAssetMenu(fileName = "BasicLocalization", menuName = "Localization/Basic Localization")]
    public class BasicLocalization : ScriptableObject
    {
        public delegate void LanguageUpdated();

        private const string DefaultDisplayName = "English";

        public string localizationID = "";
        public string displayName = DefaultDisplayName;

        public BasicTranslate[] translates =
        {
        };

        protected virtual void OnValidate()
        {
            // Maybe fix random data lost issue or doing nothing
            DontDestroyOnLoad(this);
            if (string.IsNullOrEmpty(displayName))
                displayName = DefaultDisplayName;
            OnLanguageUpdated?.Invoke();
        }

        public static event LanguageUpdated? OnLanguageUpdated;
    }
}