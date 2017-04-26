using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class DateTimeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var dt = (DateTime)value;
            return dt.Equals(DateTime.MinValue) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
