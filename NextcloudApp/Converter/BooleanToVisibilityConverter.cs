using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var invert = parameter != null;
            if (value is bool && (bool)value)
            {
                return !invert ? Visibility.Visible : Visibility.Collapsed;
            }
            return !invert ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var invert = parameter != null;
            if (value is Visibility && (Visibility)value == Visibility.Visible)
            {
                return !invert;
            }
            return invert;
        }
    }
}
