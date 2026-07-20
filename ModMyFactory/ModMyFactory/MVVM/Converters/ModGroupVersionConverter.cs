using System;
using System.Globalization;
using System.Windows.Data;
using ModMyFactory.Helpers;

namespace ModMyFactory.MVVM.Converters
{
    [ValueConversion(typeof(Version), typeof(string))]
    sealed class ModGroupVersionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var version = value as Version;
            if (version == null) throw new ArgumentException("Value has to be of type Version.", nameof(value));

            var normalized = FactorioVersionHelper.Normalize(version);

            // Factorio 1.0 uses the same mods as 0.18, so show both labels for that group.
            if (normalized == new Version(0, 18))
                return "1.0 (0.18)";

            return normalized.ToString(2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
