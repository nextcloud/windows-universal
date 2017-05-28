using NextcloudApp.Constants;
using NextcloudApp.Services;
using NextcloudApp.Utils;
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class SynchronizeThisFolderToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var item = DirectoryService.Instance.PathStack.Count > 0 ? DirectoryService.Instance.PathStack[DirectoryService.Instance.PathStack.Count - 1].ResourceInfo : null;

            if (item == null)
                return String.Empty;

            if (SyncDbUtils.IsSynced(item))
            {
                return ResourceLoader.GetForCurrentView().GetString(ResourceConstants.SynchronizeThisFolderNow);
            }
            else
            {
                return ResourceLoader.GetForCurrentView().GetString(ResourceConstants.SynchronizeThisFolderStart);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
