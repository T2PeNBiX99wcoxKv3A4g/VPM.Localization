using System;
using JetBrains.Annotations;

namespace io.github.ykysnk.Localization
{
    [Serializable]
    [PublicAPI]
    public struct BasicTranslate
    {
        public string key;
        public string translate;
        public string tooltip;
    }
}