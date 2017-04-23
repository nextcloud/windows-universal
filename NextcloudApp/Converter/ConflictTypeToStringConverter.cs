using NextcloudApp.Models;
using NextcloudClient.Types;
using System;
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
            return SyncConflict.GetConflictMessage((ConflictType)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return new ResourceInfo();
        }
    }
}
