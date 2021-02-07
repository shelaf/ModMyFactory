﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ModMyFactory.Models;
using ModMyFactory.ModSettings;

namespace ModMyFactory
{
    class ModCollection : ObservableCollection<Mod>
    {
        private bool updating;

        /// <summary>
        /// Checks if the collection contains a mod.
        /// </summary>
        /// <param name="name">The name of the mod.</param>
        /// <returns>Returns true if the collection contains the mod, otherwise false.</returns>
        public bool Contains(string name)
        {
            return this.Any(mod => string.Equals(mod.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Checks if the collection contains a mod.
        /// </summary>
        /// <param name="name">The name of the mod.</param>
        /// <param name="version">The mods version.</param>
        /// <returns>Returns true if the collection contains the mod, otherwise false.</returns>
        public bool Contains(string name, GameCompatibleVersion version)
        {
            return this.Any(mod =>
                string.Equals(mod.Name, name, StringComparison.InvariantCultureIgnoreCase)
                && (mod.Version == version));
        }

        /// <summary>
        /// Checks if the collection contains a mod.
        /// </summary>
        /// <param name="name">The name of the mod.</param>
        /// <param name="factorioVersion">The mods Factorio version.</param>
        /// <returns>Returns true if the collection contains the mod, otherwise false.</returns>
        public bool ContainsbyFactorioVersion(string name, GameCompatibleVersion factorioVersion)
        {
            return this.Any(mod =>
                string.Equals(mod.Name, name, StringComparison.InvariantCultureIgnoreCase)
                && (mod.FactorioVersion == factorioVersion));
        }

        /// <summary>
        /// Finds mods in this collection.
        /// </summary>
        /// <param name="name">The name of the mods.</param>
        /// <returns>Returns the mods searched for.</returns>
        public IEnumerable<Mod> Find(string name)
        {
            return this.Where(mod => string.Equals(mod.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Finds mods in this collection.
        /// </summary>
        /// <param name="name">The name of the mods.</param>
        /// <param name="factorioVersion">The mods Factorio version.</param>
        /// <returns>Returns the mods searched for.</returns>
        public IEnumerable<Mod> Find(string name, GameCompatibleVersion factorioVersion)
        {
            return this.Where(mod => string.Equals(mod.Name, name, StringComparison.InvariantCultureIgnoreCase) && (mod.FactorioVersion == factorioVersion));
        }

        /// <summary>
        /// Tries to get the specified mod.
        /// </summary>
        /// <param name="name">The name of the mod.</param>
        /// <param name="version">The mods version.</param>
        /// <param name="mod">Out. The requested mod.</param>
        /// <returns>Return true if the collection contained the specified mod, otherwise false.</returns>
        public bool TryGetMod(string name, GameCompatibleVersion version, out Mod mod)
        {
            foreach (var m in this)
            {
                if (string.Equals(m.Name, name, StringComparison.InvariantCultureIgnoreCase) && (m.Version == version))
                {
                    mod = m;
                    return true;
                }
            }

            mod = null;
            return false;
        }

        /// <summary>
        /// Evaluates the dependencies of all mods in the collection.
        /// </summary>
        private void EvaluateDependencies()
        {
            if (!updating)
            {
                foreach (var mod in this)
                    mod.EvaluateDependencies();
            }
        }

        private void LoadSettings()
        {
            if (!updating)
            {
                foreach (var mod in this)
                {
                    if (!mod.HasUnsatisfiedDependencies)
                        mod.LoadSettings();
                }
            }
        }

        public void BeginUpdate()
        {
            updating = true;
        }

        public void EndUpdate()
        {
            updating = false;
            EvaluateDependencies();
            LoadSettings();
            ModSettingsManager.SaveBinarySettings(this);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            bool active = item.Active;

            base.RemoveItem(index);

            if (!updating)
            {
                EvaluateDependencies();
                if (active) ModSettingsManager.SaveBinarySettings(this);
            }
        }

        protected override void InsertItem(int index, Mod item)
        {
            base.InsertItem(index, item);

            if (!updating)
            {
                EvaluateDependencies();
                LoadSettings();
                if (item.Active) ModSettingsManager.SaveBinarySettings(this);
            }
        }

        protected override void SetItem(int index, Mod item)
        {
            base.SetItem(index, item);

            if (!updating)
            {
                EvaluateDependencies();
                LoadSettings();
                if (item.Active) ModSettingsManager.SaveBinarySettings(this);
            }
        }
    }
}
