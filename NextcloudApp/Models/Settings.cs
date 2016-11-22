using System.ComponentModel;
using System.Runtime.CompilerServices;
using NextcloudApp.Annotations;
using NextcloudApp.Utils;

namespace NextcloudApp.Models
{
    public class Settings : INotifyPropertyChanged
    {
        private string _serverAddress;
        private string _username;
        private bool _showFileAndFolderGroupingHeader = true;
        private PreviewImageDownloadMode _previewImageDownloadMode = PreviewImageDownloadMode.Always;
        private int _appTotalRuns;
        private string _appRunsAfterLastUpdateVersion;
        private int _appRunsAfterLastUpdate;

        public string ServerAddress
        {
            get { return _serverAddress; }
            set
            {
                if (value == null)
                {
                    value = "";
                }
                if (_serverAddress == value)
                {
                    return;
                }
                _serverAddress = value;
                OnPropertyChanged();
            }
        }

        public string Username
        {
            get { return _username; }
            set
            {
                if (value == null)
                {
                    value = "";
                }
                if (_username == value)
                {
                    return;
                }
                _username = value;
                OnPropertyChanged();
            }
        }

        public bool ShowFileAndFolderGroupingHeader
        {
            get { return _showFileAndFolderGroupingHeader; }
            set
            {
                if (_showFileAndFolderGroupingHeader == value)
                {
                    return;
                }
                _showFileAndFolderGroupingHeader = value;
                OnPropertyChanged();
            }
        }

        public PreviewImageDownloadMode PreviewImageDownloadMode
        {
            get { return _previewImageDownloadMode; }
            set
            {
                if (_previewImageDownloadMode == value)
                {
                    return;
                }
                _previewImageDownloadMode = value;
                OnPropertyChanged();
            }
        }

        public int AppTotalRuns
        {
            get { return _appTotalRuns; }
            set
            {
                if (_appTotalRuns == value)
                {
                    return;
                }
                _appTotalRuns = value;
                OnPropertyChanged();
            }
        }
        public string AppRunsAfterLastUpdateVersion
        {
            get { return _appRunsAfterLastUpdateVersion; }
            set
            {
                if (value == null)
                {
                    value = "";
                }
                if (_appRunsAfterLastUpdateVersion == value)
                {
                    return;
                }
                _appRunsAfterLastUpdateVersion = value;
                OnPropertyChanged();
            }
        }
        public int AppRunsAfterLastUpdate
        {
            get { return _appRunsAfterLastUpdate; }
            set
            {
                if (_appRunsAfterLastUpdate == value)
                {
                    return;
                }
                _appRunsAfterLastUpdate = value;
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
