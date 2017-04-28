using System;
using Windows.UI.Xaml.Data;
using NextcloudClient.Types;
using NextcloudApp.Utils;

namespace NextcloudApp.Converter
{
    public class ContentTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var item = (ResourceInfo)value;
            return item.ContentType.Equals("dav/directory") ? SyncDbUtils.IsSynced(item) ? "\uE8F7" : "\uE8B7" : "\uE8A5";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new ResourceInfo();
        }
    }
}
