using System;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml.Data;

namespace NextcloudApp.Converter
{
    public class SyncInfoStoragePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string str)
        {
            return new AsyncTask(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string str)
        {
            throw new NotImplementedException();
        }

        public class AsyncTask
        {
            public AsyncTask(object value)
            {
                AsyncValue = null;
                LoadValue(value);
            }

            private async void LoadValue(object value)
            {
                var accessListKey = value.ToString();
                var tempFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(accessListKey);
                AsyncValue = tempFolder.Path;
            }

            public string AsyncValue { get; set; }
        }
    }
}
