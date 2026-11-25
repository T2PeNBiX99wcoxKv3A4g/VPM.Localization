using System;
using JetBrains.Annotations;

namespace io.github.ykysnk.Localization
{
    /// <summary>
    ///     One translation entry: the key, its text, and the optional tooltip.
    ///     <para>
    ///         Tooltips are opt-in. When <see cref="useTooltip" /> is false the text in <see cref="tooltip" /> is kept
    ///         in the asset but not loaded or shown. <see cref="tooltipSuffix" /> selects the key suffix used for the
    ///         tooltip entry (":tooltip" by default, matching the other packages in this project).
    ///     </para>
    /// </summary>
    [Serializable]
    [PublicAPI]
    public class BasicTranslate
    {
        /// <summary>Suffix appended to <see cref="key" /> unless an entry overrides it.</summary>
        public const string DefaultTooltipSuffix = ":tooltip";

        /// <summary>Key that identifies the translation, e.g. "label.language".</summary>
        public string key = "";

        /// <summary>Translated text for <see cref="key" />.</summary>
        public string translate = "";

        /// <summary>Tooltip text, used only when <see cref="useTooltip" /> is true.</summary>
        public string tooltip = "";

        /// <summary>Whether the tooltip takes part in loading and in the editor UI. Off by default.</summary>
        public bool useTooltip;

        /// <summary>
        ///     Suffix appended to <see cref="key" /> to publish the tooltip, e.g. "label.language:tooltip". Leave it at
        ///     <see cref="DefaultTooltipSuffix" /> unless a plugin expects another spelling such as ".tooltip".
        /// </summary>
        public string tooltipSuffix = DefaultTooltipSuffix;
    }
}