using ModMyFactory.ViewModels;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ModMyFactory.MVVM.Converters
{
    [ValueConversion(typeof(GameCompatibleVersion), typeof(string))]
    sealed class FactorioVersionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var version = value as GameCompatibleVersion;
            if (version == null) throw new ArgumentException("Value has to be of type Version.", nameof(value));

            if (version == OnlineModsViewModel.EmptyVersion)
                return App.Instance.GetLocalizedResourceString("EmptyVersionFormat");

            return $"Factorio {version.ToString(2)}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
