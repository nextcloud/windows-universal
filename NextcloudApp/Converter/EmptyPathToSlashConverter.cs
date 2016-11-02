using System;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class EmptyPathToSlashConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return string.IsNullOrEmpty((string) value) ? "/" : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (string) value == "/" ? "" : value;
        }
    }
}
