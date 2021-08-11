﻿using ModMyFactory.Models;
using ModMyFactory.Models.ModSettings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ModMyFactory.ModSettings.Serialization
{
    class ModSettingsExportTemplate
    {
        [JsonProperty("startup")]
        public Dictionary<string, ModSettingValueTemplate> StartupTemplates { get; }

        [JsonProperty("runtime-global")]
        public Dictionary<string, ModSettingValueTemplate> RuntimeGlobalTemplates { get; }

        [JsonProperty("runtime-per-user")]
        public Dictionary<string, ModSettingValueTemplate> RuntimeUserTemplates { get; }

        public ModSettingsExportTemplate()
        {
            StartupTemplates = new Dictionary<string, ModSettingValueTemplate>();
            RuntimeGlobalTemplates = new Dictionary<string, ModSettingValueTemplate>();
            RuntimeUserTemplates = new Dictionary<string, ModSettingValueTemplate>();
        }

        public ModSettingsExportTemplate(IHasModSettings mod)
            : this()
        {
            AddMod(mod);
        }

        [JsonConstructor]
        public ModSettingsExportTemplate(Dictionary<string, ModSettingValueTemplate> startupTemplates, Dictionary<string, ModSettingValueTemplate> runtimeGlobalTemplates, Dictionary<string, ModSettingValueTemplate> runtimeUserTemplates)
        {
            StartupTemplates = startupTemplates;
            RuntimeGlobalTemplates = runtimeGlobalTemplates;
            RuntimeUserTemplates = runtimeUserTemplates;
        }

        public void AddSetting(IModSetting setting)
        {
            var template = setting.CreateValueTemplate();

            switch (setting.LoadTime)
            {
                case LoadTime.Startup:
                    StartupTemplates[setting.Name] = template;
                    break;
                case LoadTime.RuntimeGlobal:
                    RuntimeGlobalTemplates[setting.Name] = template;
                    break;
                case LoadTime.RuntimeUser:
                    RuntimeUserTemplates[setting.Name] = template;
                    break;
            }
        }

        public void AddMod(IHasModSettings mod)
        {
            if (mod.Settings != null)
            {
                foreach (var setting in mod.Settings)
                    AddSetting(setting);
            }
        }

        public bool TryGetValue<T>(IModSetting<T> setting, out T value) where T : IEquatable<T>
        {
            Dictionary<string, ModSettingValueTemplate> dict;

            switch (setting.LoadTime)
            {
                case LoadTime.Startup:
                    dict = StartupTemplates;
                    break;
                case LoadTime.RuntimeGlobal:
                    dict = RuntimeGlobalTemplates;
                    break;
                case LoadTime.RuntimeUser:
                    dict = RuntimeUserTemplates;
                    break;
                default:
                    value = default(T);
                    return false;
            }
            
            bool hasValue = dict.TryGetValue(setting.Name, out var template);
            if (hasValue && (template != null))
            {
                try
                {
                    value = template.GetValue<T>();
                    return true;
                }
                catch (FormatException)
                {
                    value = default(T);
                    return false;
                }
            }
            else
            {
                value = default(T);
                return false;
            }
        }
    }
}
