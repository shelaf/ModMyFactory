using System.Collections.Generic;

namespace ModMyFactory.Web.UpdateApi
{
    /// <summary>
    /// Maps the JSON returned by updater.factorio.com/get-available-versions.
    /// The root object contains package name (e.g. "core-win64", "space-age-win64") -> update steps.
    /// </summary>
    sealed class UpdateInfoTemplate : Dictionary<string, UpdateStepTemplate[]>
    {
    }
}
