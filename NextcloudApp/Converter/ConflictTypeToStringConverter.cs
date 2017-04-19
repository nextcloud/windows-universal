using NextcloudApp.Models;
using NextcloudApp.Utils;
using NextcloudClient.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    /// <summary>
    /// Checks if the folder is Synchronized or not
    /// </summary>
    public class ConflictTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return SyncConflict.GetConflictMessage((ConflictType) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new ResourceInfo();
        }
    }
}
