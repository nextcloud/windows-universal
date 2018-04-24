using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NextcloudApp.Annotations;
using NextcloudApp.Services;

namespace NextcloudApp.Controls
{
    public sealed partial class FilesAndFoldersStatusBar : INotifyPropertyChanged
    {
        public FilesAndFoldersStatusBar()
        {
            InitializeComponent();
            DirectoryService.Instance.PropertyChanged += InstanceOnPropertyChanged;
        }

        private void InstanceOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            OnPropertyChanged(nameof(StatusBarText));
        }

        public string StatusBarText
        {
            get
            {
                var folderCount = DirectoryService.Instance?.FilesAndFolders.Count(x => x.IsDirectory);
                var fileCount = DirectoryService.Instance?.FilesAndFolders.Count(x => !x.IsDirectory);
                return string.Format(LocalizationService.Instance?.GetString("DirectoryListStatusBarText"), fileCount + folderCount, folderCount, fileCount);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
