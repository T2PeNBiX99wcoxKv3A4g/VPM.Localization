using System;
using io.github.ykysnk.Localization.Editor;
using JetBrains.Annotations;

[assembly: LocalizationPackage("io.github.ykysnk.localization")]

namespace io.github.ykysnk.Localization.Editor
{
    /// <summary>
    ///     Declares which localization an assembly's editor UI uses. Declare it once per assembly, next to the
    ///     <c>using</c> directives:
    ///     <code>[assembly: LocalizationPackage("io.github.ykysnk.yky-toolkit")]</code>
    ///     After that, <see cref="LocalizationExtensions" /> resolves the right helper on its own, so a package needs
    ///     neither a wrapper class nor a helper field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    [PublicAPI]
    public sealed class LocalizationPackageAttribute : Attribute
    {
        /// <summary>Declares the localization id of the assembly this attribute is placed on.</summary>
        public LocalizationPackageAttribute(string localizationID) => LocalizationID = localizationID;

        /// <summary>Localization id whose assets hold this assembly's translations.</summary>
        public string LocalizationID { get; }
    }
}