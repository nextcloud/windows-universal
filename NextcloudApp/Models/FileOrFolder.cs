using System.ComponentModel;
using System.Runtime.CompilerServices;
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
            Id = item.Id;
            FileId = item.FileId;
            IsFavorite = item.IsFavorite;
            CommentsHref = item.CommentsHref;
            CommentsCount = item.CommentsCount;
            CommentsUnread = item.CommentsUnread;
            OwnderId = item.OwnderId;
            OwnerDisplayName = item.OwnerDisplayName;
            ShareTypes = item.ShareTypes;
            Checksums = item.Checksums;
            HasPreview = item.HasPreview;
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
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
