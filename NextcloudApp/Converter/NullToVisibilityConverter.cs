using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var invert = parameter != null;
            return value == null
                ? (invert ? Visibility.Visible : Visibility.Collapsed)
                : (invert ? Visibility.Collapsed : Visibility.Visible);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
