using NextcloudApp.Services;
using NextcloudApp.Utils;
using System;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    /// <summary>
    /// Checks if the current folder is synchronized or not
    /// </summary>
    public class IsThisFolderSyncedToIsEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var item = DirectoryService.Instance.PathStack.Count > 0 ? DirectoryService.Instance.PathStack[DirectoryService.Instance.PathStack.Count - 1].ResourceInfo : null;

            if (item == null)
                return false;

            if (item.ContentType == null || !item.ContentType.Equals("dav/directory"))
            {
                return false;
            }

            if (SyncDbUtils.IsSynced(item))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
