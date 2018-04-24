using NextcloudApp.Models;
using NextcloudApp.Services;

namespace NextcloudApp.Controls
{
    public sealed partial class FileOrFolderListZoomedOutView
    {
        public FileOrFolderListZoomedOutView()
        {
            InitializeComponent();
        }
        public DirectoryService Directory => DirectoryService.Instance;

        public LocalSettings Settings => SettingsService.Default.Value.LocalSettings;
    }
}
