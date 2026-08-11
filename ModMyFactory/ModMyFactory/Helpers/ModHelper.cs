using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using ModMyFactory.ViewModels;
using ModMyFactory.Views;
using ModMyFactory.Web;
using ModMyFactory.Web.ModApi;

namespace ModMyFactory.Helpers
{
    static class ModHelper
    {
        public static async Task<List<ModInfo>> FetchMods(Window progressOwner, ModCollection installedMods)
        {
            var progressWindow = new ProgressWindow() { Owner = progressOwner };
            var progressViewModel = (ProgressViewModel)progressWindow.ViewModel;
            progressViewModel.ActionName = App.Instance.GetLocalizedResourceString("FetchingModsAction");
            progressViewModel.CanCancel = false;
            progressViewModel.IsIndeterminate = true;

            Task<List<ModInfo>> fetchModsTask = ModWebsite.GetModsAsync(installedMods);

            await progressWindow.ShowProgressAsync(fetchModsTask);

            List<ModInfo> modInfos = await fetchModsTask;

            return modInfos;
        }
    }
}
