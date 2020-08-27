using System;
using System.Globalization;
using System.Windows.Data;

namespace ModMyFactory.MVVM.Converters
{
    internal sealed class FactorioMajorVersionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var version = (GameCompatibleVersion)value;
            if ((version.Major == 1 && version.Minor == 0) || (version.Major == 0 && version.Minor == 18))
            {
                return "1.0 (0.18)";
            }
            return version.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
