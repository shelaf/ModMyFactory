using System.Windows;
using System.Windows.Controls;
using ModMyFactory.Models.ModSettings;

namespace ModMyFactory.MVVM.Selectors
{
    sealed class ModSettingTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var setting = item as IModSetting;
            if (setting != null) return setting.Template;

            return base.SelectTemplate(item, container);
        }
    }
}
