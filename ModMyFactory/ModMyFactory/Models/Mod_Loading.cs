using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModMyFactory.Models
{
    partial class Mod
    {
        private static void AddFileToDictionary(Dictionary<string, ModFileCollection> dictionary, FileSystemInfo file)
        {
            if (ModFile.TryLoad(file, out var modFile))
            {
                string key = $"{modFile.Name}_{modFile.Version}";

                ModFileCollection collection;
                if (!dictionary.TryGetValue(key, out collection))
                {
                    collection = new ModFileCollection();
                    dictionary.Add(key, collection);
                }

                collection.Add(modFile);
            }
        }

        private static Dictionary<string, ModFileCollection> CreateFileDictionary(IEnumerable<DirectoryInfo> directories)
        {
            var dictionary = new Dictionary<string, ModFileCollection>();

            foreach (var directory in directories)
            {
                foreach (var file in directory.EnumerateFileSystemInfos())
                    AddFileToDictionary(dictionary, file);
            }

            return dictionary;
        }

        private static void LoadModsFromFileDictionary(Dictionary<string, ModFileCollection> fileDictionary, ModCollection parentCollection, ModpackCollection modpackCollection)
        {
            foreach (var modFileList in fileDictionary.Select(kvp => kvp.Value))
            {
                var mod = new Mod(modFileList, parentCollection, modpackCollection);
                parentCollection.Add(mod);
            }
        }

        /// <summary>
        /// Loads all mods from the selected mod directory into the specified parent collection.
        /// </summary>
        /// <param name="parentCollection">The collection to contain the mods.</param>
        /// <param name="modpackCollection">The collection containing all modpacks.</param>
        public static void LoadMods(ModCollection parentCollection, ModpackCollection modpackCollection)
        {
            parentCollection.BeginUpdate();

            var modDirectory = App.Instance.Settings.GetModDirectory();
            if (!modDirectory.Exists) modDirectory.Create();

            var selectedDirectories = new List<DirectoryInfo>();
            foreach (var subDirectory in modDirectory.EnumerateDirectories())
            {
                if (System.Version.TryParse(subDirectory.Name, out var factorioVersion))
                    selectedDirectories.Add(subDirectory);
            }

            var fileDictionary = CreateFileDictionary(selectedDirectories);
            LoadModsFromFileDictionary(fileDictionary, parentCollection, modpackCollection);

            parentCollection.EndUpdate();
        }

        // The 'core' folder is always loaded by Factorio, is not listed in mod-list.json and
        // cannot be toggled, so it is not represented as a mod.
        private static readonly string[] HiddenOfficialMods = { "core" };

        /// <summary>
        /// Removes all currently loaded official mods from the collection.
        /// </summary>
        public static void UnloadOfficialMods(ModCollection parentCollection)
        {
            parentCollection.BeginUpdate();

            foreach (var mod in parentCollection.Where(mod => mod.IsOfficial).ToList())
                parentCollection.Remove(mod);

            parentCollection.EndUpdate();
        }

        /// <summary>
        /// Loads the official mods shipped inside a Factorio installation's data folder into the collection.
        /// Official mods depend on the selected Factorio version and reside in the installation, not the managed mod directory.
        /// </summary>
        /// <param name="factorioVersion">The selected Factorio version whose official mods are loaded.</param>
        /// <param name="parentCollection">The collection to contain the mods.</param>
        /// <param name="modpackCollection">The collection containing all modpacks.</param>
        public static void LoadOfficialMods(FactorioVersion factorioVersion, ModCollection parentCollection, ModpackCollection modpackCollection)
        {
            if ((factorioVersion?.Directory == null) || !factorioVersion.Directory.Exists) return;

            var dataDirectory = new DirectoryInfo(Path.Combine(factorioVersion.Directory.FullName, "data"));
            if (!dataDirectory.Exists) return;

            parentCollection.BeginUpdate();

            foreach (var directory in dataDirectory.EnumerateDirectories())
            {
                if (HiddenOfficialMods.Contains(directory.Name)) continue;
                if (!ModFile.TryLoadOfficial(directory, out var modFile)) continue;

                var mod = new Mod(modFile, parentCollection, modpackCollection);
                parentCollection.Add(mod);
            }

            parentCollection.EndUpdate();
        }
    }
}
