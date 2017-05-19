using NextcloudClient.Types;
using System;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class FavoriteToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var item = (ResourceInfo)value;
            return item.IsFavorite ? "\uE735" : "\uE734";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new ResourceInfo();
        }
    }
}
