using System;
using System.Globalization;
using System.Windows.Data;

namespace ModMyFactory.Helpers
{
    public class GroupNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            GameCompatibleVersion version = (GameCompatibleVersion)value;
            if ((version.Major == 1 && version.Minor == 0) || (version.Major == 0 && version.Minor == 18))
            {
                return "1.0 (0.18)";
            }
            return version.ToString();

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((string)value == "1.0 (0.18)")
            {
                return new GameCompatibleVersion(0, 18);
            }
            else
            {
                return new GameCompatibleVersion((string)value);
            }
        }
    }
}
