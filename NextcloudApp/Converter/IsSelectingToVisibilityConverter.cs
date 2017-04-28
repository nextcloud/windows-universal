using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using NextcloudClient.Types;

namespace NextcloudApp.Converter
{
    public class IsSelectingToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var invert = parameter != null;
            if (!invert)
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new ResourceInfo();
        }
    }
}
