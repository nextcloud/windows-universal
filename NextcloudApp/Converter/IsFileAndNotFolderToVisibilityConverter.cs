using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using NextcloudClient.Types;

namespace NextcloudApp.Converter
{
    public class IsFileAndNotFolderToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var invert = parameter != null;
            var item = (ResourceInfo)value;
            if (invert)
            {
                return item.ContentType.Equals("dav/directory") ? Visibility.Visible : Visibility.Collapsed;
            }
            return item.ContentType.Equals("dav/directory") ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new ResourceInfo();
        }
    }
}
