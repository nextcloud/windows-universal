using NextcloudApp.Models;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class ConflictTypeNoneToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var conflictType = (ConflictType) value;
            var inverse = parameter != null && bool.Parse(parameter.ToString());
            
            if (inverse)
            {
                return conflictType == ConflictType.None ? Visibility.Collapsed : Visibility.Visible;
            }
            return conflictType == ConflictType.None ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
