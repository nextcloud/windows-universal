using NextcloudApp.Services;
using NextcloudApp.Utils;
using NextcloudClient.Types;
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    /// <summary>
    /// Checks if the current folder is synchronized or not
    /// </summary>
    public class IsThisFolderSyncedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var item = DirectoryService.Instance.PathStack.Count > 0 ? DirectoryService.Instance.PathStack[DirectoryService.Instance.PathStack.Count - 1].ResourceInfo : null;

            if (item == null)
                return Visibility.Visible;

            if (item.ContentType == null || !item.ContentType.Equals("dav/directory"))
            {
                return Visibility.Collapsed;
            }

            if (SyncDbUtils.IsSynced(item))
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new ResourceInfo();
        }
    }
}
