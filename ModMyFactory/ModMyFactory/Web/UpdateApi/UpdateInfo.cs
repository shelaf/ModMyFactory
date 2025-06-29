using System;

namespace ModMyFactory.Web.UpdateApi
{
    sealed class UpdateInfo
    {
        public Package Package { get; }

        public UpdateInfo(UpdateInfoTemplate template, bool isExpansion)
        {
            var packageTemplate = Environment.Is64BitOperatingSystem
                ? (isExpansion ? template.ExpansionPackage : template.Win64Package)
                : template.Win32Package;

            Package = packageTemplate != null ? new Package(packageTemplate, isExpansion) : null;
        }
    }
}
