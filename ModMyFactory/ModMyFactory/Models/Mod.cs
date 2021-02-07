﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ModMyFactory.Helpers;
using ModMyFactory.Models.ModSettings;
using ModMyFactory.ModSettings;
using ModMyFactory.MVVM.Sorters;
using ModMyFactory.ViewModels;
using ModMyFactory.Views;
using WPFCore;
using WPFCore.Commands;

namespace ModMyFactory.Models
{
    /// <summary>
    /// A mod.
    /// </summary>
    sealed partial class Mod : NotifyPropertyChangedBase, IHasModSettings
    {
        private readonly ModCollection parentCollection;
        private readonly ModpackCollection modpackCollection;
        private readonly ModFileCollection oldVersions;

        bool active;
        bool isSelected;
        bool hasUnsatisfiedDependencies;
        ModFile file;

        private void SetInactiveFileDisabled()
        {
            if (active)
            {
                active = false;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Active)));
            }

            File.Disable();
            foreach (var oldFile in oldVersions) oldFile.Disable();
        }

        private bool IsOnly => !parentCollection.Find(Name, FactorioVersion).Where(mod => mod != this).Any();

        private bool IsDefault
        {
            get
            {
                if (IsOnly) return true;

                var candidates = parentCollection.Find(Name, FactorioVersion);
                var max = candidates.MaxBy(mod => mod.Version, new VersionComparer());
                return max == this;
            }
        }

        /// <summary>
        /// Indicates whether the mod is currently active.
        /// </summary>
        public bool Active
        {
            get { return active; }
            set
            {
                if (value != active)
                {
                    active = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Active)));

                    ModManager.SetActive(Name, Version, FactorioVersion, value, IsOnly, IsDefault);

                    ModSettingsManager.BeginUpdate();

                    if (active)
                    {
                        File.Enable();
                        foreach (var oldFile in oldVersions) oldFile.Enable();

                        var mods = parentCollection.Find(Name, FactorioVersion);
                        foreach (var mod in mods)
                        {
                            if (mod != this)
                                mod.SetInactiveFileDisabled();
                        }
                    }
                    
                    if (active && App.Instance.Settings.ActivateDependencies)
                        ActivateDependencies(App.Instance.Settings.ActivateOptionalDependencies);

                    ModSettingsManager.EndUpdate();
                    ModSettingsManager.SaveBinarySettings(parentCollection);
                }
            }
        }

        /// <summary>
        /// Indicates whether this mod is selected in the list.
        /// </summary>
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (value != isSelected)
                {
                    isSelected = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        /// <summary>
        /// Indicates whether this mod has unsatisfied dependencies.
        /// </summary>
        public bool HasUnsatisfiedDependencies
        {
            get => hasUnsatisfiedDependencies;
            private set
            {
                if (value != hasUnsatisfiedDependencies)
                {
                    hasUnsatisfiedDependencies = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasUnsatisfiedDependencies)));
                }
            }
        }

        /// <summary>
        /// The mods file.
        /// </summary>
        public ModFile File
        {
            get => file;
            private set
            {
                if (value != file)
                {
                    file = value;

                    var source = new CollectionViewSource() { Source = Dependencies };
                    var dependenciesView = (ListCollectionView)source.View;
                    dependenciesView.CustomSort = new ModDependencySorter();
                    dependenciesView.Filter = (item) => !((ModDependency)item).IsHidden;
                    DependenciesView = dependenciesView;

                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Version)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(FactorioVersion)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(FriendlyName)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Author)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Description)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Dependencies)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(DependenciesView)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasVisibleDependencies)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Thumbnail)));
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasThumbnail)));
                }
            }
        }

        private InfoFile InfoFile => File.InfoFile;

        /// <summary>
        /// The unique name of the mod.
        /// </summary>
        public string Name => InfoFile.Name;

        /// <summary>
        /// The version of the mod.
        /// </summary>
        public GameCompatibleVersion Version => InfoFile.Version;

        /// <summary>
        /// The version of Factorio this mod is compatible with, where version 1.0 is considered to be version 0.18 for compatibility reasons
        /// </summary>
        public GameCompatibleVersion FactorioVersion => InfoFile.FactorioVersion;

        /// <summary>
        /// The friendly of the mod.
        /// </summary>
        public string FriendlyName => InfoFile.FriendlyName;

        /// <summary>
        /// The author of the mod.
        /// </summary>
        public string Author => InfoFile.Author;

        /// <summary>
        /// The description of the mod.
        /// </summary>
        public string Description => InfoFile.Description;

        /// <summary>
        /// This mods dependencies.
        /// </summary>
        public ModDependency[] Dependencies => InfoFile.Dependencies;

        /// <summary>
        /// A view containing this mods dependencies.
        /// </summary>
        public ICollectionView DependenciesView { get; private set; }

        /// <summary>
        /// Indicates whether this mod has visible dependencies.
        /// </summary>
        public bool HasVisibleDependencies => Dependencies.Any(item => !item.IsHidden);

        /// <summary>
        /// This mods settings.
        /// </summary>
        public IReadOnlyCollection<IModSetting> Settings { get; private set; }

        /// <summary>
        /// A view containing this mods settings.
        /// </summary>
        public ICollectionView SettingsView { get; private set; }

        /// <summary>
        /// Indicates whether this mod has any settings.
        /// </summary>
        public bool HasSettings => (Settings == null) ? false : (Settings.Count > 0);

        /// <summary>
        /// Indicates whether updates for this mod should be extracted.
        /// </summary>
        public bool ExtractUpdates => File?.ExtractUpdates ?? false;

        /// <summary>
        /// An optional thumbnail provided in the mod file.
        /// </summary>
        public BitmapImage Thumbnail => File.Thumbnail;

        /// <summary>
        /// Indicates whether this mod is providing a thumbnail.
        /// </summary>
        public bool HasThumbnail => Thumbnail != null;

        /// <summary>
        /// Indicates if all of this mods required dependencies are active.
        /// </summary>
        public bool DependenciesActive
        {
            get
            {
                foreach (var dependency in Dependencies)
                {
                    if (!dependency.IsOptional)
                    {
                        if (!dependency.IsActive(parentCollection, FactorioVersion))
                            return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// A command that deletes this mod from the list and the filesystem.
        /// </summary>
        public RelayCommand<bool?> DeleteCommand { get; }

        string IHasModSettings.DisplayName => $"{FriendlyName} ({FactorioVersion})";

        string IHasModSettings.UniqueID => $"{Name}_{Version}";

        bool IHasModSettings.UseBinaryFileOverride => true;

        bool IHasModSettings.Override
        {
            get => true;
            set { }
        }

        public ICommand ViewSettingsCommand { get; }

        private Mod(ModCollection parentCollection, ModpackCollection modpackCollection)
        {
            this.parentCollection = parentCollection;
            this.modpackCollection = modpackCollection;

            DeleteCommand = new RelayCommand<bool?>(showPrompt => Delete(showPrompt ?? true));
            ViewSettingsCommand = new RelayCommand(ViewSettings);
        }

        /// <summary>
        /// Creates a mod.
        /// </summary>
        private Mod(ModFileCollection files, ModCollection parentCollection, ModpackCollection modpackCollection)
            : this(parentCollection, modpackCollection)
        {
            File = files.Latest;
            files.Remove(file);
            oldVersions = files;

            if (!File.Enabled) active = false;
            else active = ModManager.GetActive(Name, Version, FactorioVersion, IsOnly);
            if (active)
            {
                File.Enable();
                foreach (var oldFile in oldVersions) oldFile.Enable();

                var mods = parentCollection.Find(Name, FactorioVersion);
                foreach (var mod in mods)
                {
                    if (mod != this)
                        mod.SetInactiveFileDisabled();
                }
            }
        }

        /// <summary>
        /// Creates a mod.
        /// </summary>
        private Mod(ModFile file, ModCollection parentCollection, ModpackCollection modpackCollection)
            : this(parentCollection, modpackCollection)
        {
            File = file;
            oldVersions = new ModFileCollection();

            if (!File.Enabled) active = false;
            else active = ModManager.GetActive(Name, Version, FactorioVersion, IsOnly);
            if (active)
            {
                File.Enable();
                foreach (var oldFile in oldVersions) oldFile.Enable();

                var mods = parentCollection.Find(Name, FactorioVersion);
                foreach (var mod in mods)
                {
                    if (mod != this)
                        mod.SetInactiveFileDisabled();
                }
            }
        }

        public void LoadSettings()
        {
            var settings = file.GetSettings(parentCollection, this);
            Settings = new ReadOnlyCollection<IModSetting>(settings);
            var source = new CollectionViewSource() { Source = Settings };
            var settingsView = (ListCollectionView)source.View;
            settingsView.CustomSort = new ModSettingSorter();
            settingsView.GroupDescriptions.Add(new PropertyGroupDescription("LoadTime"));
            SettingsView = settingsView;

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Settings)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(SettingsView)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasSettings)));
        }
        
        public ILocale GetLocale(CultureInfo culture)
        {
            if (File == null) return new ModLocale(culture);
            return File.GetLocale(culture);
        }

        /// <summary>
        /// Activates this mods dependencies.
        /// </summary>
        /// <param name="optional">Indicates whether optional dependencies should also be activated.</param>
        public void ActivateDependencies(bool optional)
        {
            foreach (var dependency in Dependencies)
            {
                if (!dependency.IsHidden && (optional || !dependency.IsOptional))
                    dependency.Activate(parentCollection, FactorioVersion);
            }
        }
        
        /// <summary>
        /// Evaluates the dependencies of this mod.
        /// </summary>
        public void EvaluateDependencies()
        {
            bool result = false;

            if ((Dependencies == null) || (Dependencies.Length == 0))
            {
                result = false;
            }
            else
            {
                foreach (var dependency in Dependencies)
                {
                    if (!dependency.IsOptional && !dependency.IsPresent(parentCollection, FactorioVersion))
                    {
                        result = true;
                    }
                    else if (dependency.IsOptional)
                    {
                        dependency.IsPresent(parentCollection, FactorioVersion);
                    }
                }
            }

            HasUnsatisfiedDependencies = result;
        }

        public void ViewSettings()
        {
            var settingsWindow = new ModSettingsWindow() { Owner = App.Instance.MainWindow };
            var settingsViewModel = (ModSettingsViewModel)settingsWindow.ViewModel;
            settingsViewModel.SetMod(this);
            settingsWindow.ShowDialog();

            ModSettingsManager.SaveSettings(this);
            if (Active) ModSettingsManager.SaveBinarySettings(parentCollection);
        }
        
        private void DeleteOldVersions()
        {
            foreach (var file in oldVersions)
                file.Delete();
        }

        /// <summary>
        /// Deletes this mod from the list and the filesystem.
        /// </summary>
        /// <param name="showPrompt">Indicates whether a confirmation prompt is shown to the user.</param>
        public void Delete(bool showPrompt)
        {
            if (!showPrompt || (MessageBox.Show(
                App.Instance.GetLocalizedMessage("DeleteMod", MessageType.Question),
                App.Instance.GetLocalizedMessageTitle("DeleteMod", MessageType.Question),
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes))
            {
                foreach (var modpack in modpackCollection)
                {
                    ModReference reference;
                    if (modpack.Contains(this, out reference))
                        modpack.Mods.Remove(reference);
                }

                File.Delete();
                DeleteOldVersions();
                parentCollection.Remove(this);

                ModManager.RemoveTemplate(Name, FactorioVersion);
                ModpackTemplateList.Instance.Update(MainViewModel.Instance.Modpacks);
                ModpackTemplateList.Instance.Save();
            }
        }
        
        /// <summary>
        /// Moves this mod to a specified directory.
        /// </summary>
        public async Task MoveToAsync(string destination)
        {
            await File.MoveToAsync(destination);

            foreach (var file in oldVersions)
                await file.MoveToAsync(destination);
        }

        /// <summary>
        /// Exports the mods file.
        /// </summary>
        public async Task<ModFile> ExportFile(string destination, int uid = -1)
        {
            return await File.CopyToAsync(destination, uid);
        }
    }
}
