using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace io.github.ykysnk.Localization
{
    /// <summary>
    ///     One localization set: the editor-only translations of one language.
    ///     <para>
    ///         <see cref="jsonFile" /> and <see cref="poFile" /> optionally point at the sidecar files written by the
    ///         inspector, so the same text can be edited with external tools.
    ///     </para>
    /// </summary>
    [PublicAPI]
    [CreateAssetMenu(fileName = "en-US", menuName = "Localization/Basic Localization")]
    public class BasicLocalization : ScriptableObject
    {
        public delegate void LocalizationFileUpdated(string localizationID, string localizationName);

        private const string DefaultDisplayName = "English";

        /// <summary>Group id shared by every language of the same localization.</summary>
        public string localizationID = "";

        /// <summary>Name shown in the language picker.</summary>
        public string displayName = DefaultDisplayName;

        /// <summary>All translations of this language.</summary>
        public List<BasicTranslate> translates = new()
        {
            new()
            {
                key = "label.language",
                translate = "Language",
                tooltip = "Change your target language in the editor.",
                useTooltip = true
            }
        };

        /// <summary>Sidecar JSON written by <c>Export JSON</c> in the inspector.</summary>
        public TextAsset? jsonFile;

        /// <summary>Sidecar PO written by <c>Export PO</c> in the inspector.</summary>
        public TextAsset? poFile;

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(displayName))
                displayName = DefaultDisplayName;
            OnLocalizationFileUpdated?.Invoke(localizationID, name);
        }

        public static event LocalizationFileUpdated? OnLocalizationFileUpdated;
    }
}