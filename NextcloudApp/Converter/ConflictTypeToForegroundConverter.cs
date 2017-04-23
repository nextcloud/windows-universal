using NextcloudApp.Models;
using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace NextcloudApp.Converter
{
    public class ConflictTypeToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var conflictType = (ConflictType)value;

            if (conflictType != ConflictType.NONE)
                return new SolidColorBrush(Colors.Red);
            else
                return null; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
