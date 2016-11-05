using System;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class FirstCharFromStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return ((string) value).Substring(0, 1).ToUpperInvariant();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
