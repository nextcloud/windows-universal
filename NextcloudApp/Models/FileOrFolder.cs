using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using NextcloudApp.Annotations;
using NextcloudClient.Types;

namespace NextcloudApp.Models
{
    public class FileOrFolder : ResourceInfo, INotifyPropertyChanged
    {
        private BitmapImage _thumbnail;

        public FileOrFolder(ResourceInfo item)
        {
            Name = item.Name;
            Path = item.Path;
            Size = item.Size;
            ETag = item.ETag;
            ContentType = item.ContentType;
            LastModified = item.LastModified;
            Created = item.Created;
            QuotaUsed = item.QuotaUsed;
            QuotaAvailable = item.QuotaAvailable;
        }

        public BitmapImage Thumbnail
        {
            get { return _thumbnail; }
            set
            {
                if (_thumbnail == value)
                {
                    return;
                }
                _thumbnail = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual async void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            await Task.Factory.StartNew(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }, CancellationToken.None, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler,
                TaskScheduler.Default).ConfigureAwait(false);
        }
    }
}
