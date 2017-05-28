using NextcloudApp.Constants;
using NextcloudApp.Utils;
using NextcloudClient.Types;
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class SynchronizeFolderToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var item = (ResourceInfo)value;

            if (SyncDbUtils.IsSynced(item))
            {
                return ResourceLoader.GetForCurrentView().GetString(ResourceConstants.SynchronizeNow);
            }
            else
            {
                return ResourceLoader.GetForCurrentView().GetString(ResourceConstants.SynchronizeStart);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new ResourceInfo();
        }
    }
}
