using System.ComponentModel;
using System.Runtime.CompilerServices;
using NextcloudApp.Annotations;
using NextcloudApp.Services;

namespace NextcloudApp.Controls
{
    public sealed partial class PathStackHeader : INotifyPropertyChanged
    {
        public PathStackHeader()
        {
            InitializeComponent();
        }

        public DirectoryService Directory => DirectoryService.Instance;

        public int SelectedPathIndex
        {
            get => -1;
            set
            {
                if (value == -1)
                {
                    return;
                }

                if (Directory?.PathStack == null)
                {
                    return;
                }

                while (Directory.PathStack.Count > 0 && Directory.PathStack.Count > value + 1)
                {
                    Directory.PathStack.RemoveAt(Directory.PathStack.Count - 1);
                }

                OnPropertyChanged();
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
