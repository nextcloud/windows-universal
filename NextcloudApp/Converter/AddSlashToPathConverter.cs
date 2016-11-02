using System;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class AddSlashToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((string) value).EndsWith("/") ? value : value + "/";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
