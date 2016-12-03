using NextcloudApp.Utils;
using NextcloudClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    /// <summary>
    /// Checks if the folder is Synchronized or not
    /// </summary>
    public class IsFolderSyncedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var invert = parameter != null;
            var item = (ResourceInfo)value;
            if (!item.ContentType.Equals("dav/directory"))
            {
                // Option not visible to files.
                return "";
            }
            ResourceLoader loader = new ResourceLoader();
            if(SyncDbUtils.GetFolderSyncInfoByPath(item.Path) == null)
            {
               return loader.GetString("Synchronize");
            } else { }
               return loader.GetString("StopSynchronize");
            }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new ResourceInfo();
        }
    }
}
