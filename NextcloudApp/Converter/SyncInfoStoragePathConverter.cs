using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
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

        public class AsyncTask : INotifyPropertyChanged
        {
            public AsyncTask(object value)
            {
                AsyncValue = null;
                LoadValue(value);
            }

            private async Task LoadValue(object value)
            {
                string accessListKey = value.ToString();
                StorageFolder tempFolder = 
                    await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(accessListKey);
                AsyncValue = tempFolder.Path;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public string AsyncValue { get; set; }
        }
    }
}
