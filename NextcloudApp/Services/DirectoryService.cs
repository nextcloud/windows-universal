using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml.Media.Imaging;
using NextcloudApp.Annotations;
using NextcloudApp.Models;
using NextcloudApp.Utils;
using NextcloudClient.Exceptions;
using NextcloudClient.Types;

namespace NextcloudApp.Services
{
    public class DirectoryService : INotifyPropertyChanged
    {
        private static DirectoryService _instance;

        private DirectoryService()
        {
            _groupedFilesAndFolders = new ObservableGroupingCollection<string, FileOrFolder>(FilesAndFolders);
            _groupedFolders = new ObservableGroupingCollection<string, FileOrFolder>(Folders);

            // Arrange for the first time, so that the collections get filled.
            _groupedFilesAndFolders.ArrangeItems(new NameSorter(SortSequence.Asc), x => x.Name.First().ToString().ToUpper());
            _groupedFolders.ArrangeItems(new NameSorter(SortSequence.Asc), x => x.Name.First().ToString().ToUpper());
        }

        public static DirectoryService Instance => _instance ?? (_instance = new DirectoryService());

        public ObservableCollection<PathInfo> PathStack { get; } = new ObservableCollection<PathInfo>
        {
            new PathInfo
            {
                ResourceInfo = new ResourceInfo()
                {
                    Name = "Nextcloud",
                    Path = "/"
                },
                IsRoot = true
            }
        };

        public ObservableCollection<FileOrFolder> FilesAndFolders { get; } = new ObservableCollection<FileOrFolder>();
        public ObservableCollection<FileOrFolder> Folders { get; } = new ObservableCollection<FileOrFolder>();

        private readonly ObservableGroupingCollection<string, FileOrFolder> _groupedFilesAndFolders;
        private ObservableGroupingCollection<string, FileOrFolder> _groupedFolders;
        private bool _isSorting;
        private bool _continueListing;
        private bool _isSelecting;
        private string _selectionMode;

        public ObservableCollection<Grouping<string, FileOrFolder>> GroupedFilesAndFolders => _groupedFilesAndFolders.Items;
        public ObservableCollection<Grouping<string, FileOrFolder>> GroupedFolders => _groupedFolders.Items;

        private void SortList()
        {
            switch (SettingsService.Instance.LocalSettings.GroupMode)
            {
                case GroupMode.GroupByNameAscending:
                    GroupByNameAscending();
                    break;
                case GroupMode.GroupByNameDescending:
                    GroupByNameDescending();
                    break;
                case GroupMode.GroupByDateAscending:
                    GroupByDateAscending();
                    break;
                case GroupMode.GroupByDateDescending:
                    GroupByDateDescending();
                    break;
                case GroupMode.GroupBySizeAscending:
                    GroupBySizeAscending();
                    break;
                case GroupMode.GroupBySizeDescending:
                    GroupBySizeDescending();
                    break;
                case GroupMode.GroupByTypeAscending:
                    GroupByTypeAscending();                    
                    break;
                case GroupMode.GroupByTypeDescending:
                    GroupByTypeDescending();
                    break;
                default:
                    break;
            }
        }

        private void FirePropertyChangedFilesAndFolders()
        {
            OnPropertyChanged(nameof(GroupedFilesAndFolders));
            OnPropertyChanged(nameof(GroupedFolders));
        }

        public void GroupByNameAscending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new NameSorter(SortSequence.Asc), x => x.Name.First().ToString().ToUpper());
            _groupedFolders.ArrangeItems(new NameSorter(SortSequence.Asc), x => x.Name.First().ToString().ToUpper());
            FirePropertyChangedFilesAndFolders();
            IsSorting = false;
            SettingsService.Instance.LocalSettings.GroupMode = GroupMode.GroupByNameAscending;
        }

        public void GroupByNameDescending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new NameSorter(SortSequence.Desc), x => x.Name.First().ToString().ToUpper());
            _groupedFolders.ArrangeItems(new NameSorter(SortSequence.Desc), x => x.Name.First().ToString().ToUpper());
            FirePropertyChangedFilesAndFolders();
            IsSorting = false;
            SettingsService.Instance.LocalSettings.GroupMode = GroupMode.GroupByNameDescending;
        }

        public void GroupByDateAscending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new DateSorter(SortSequence.Asc), x => x.LastModified.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern));
            _groupedFolders.ArrangeItems(new DateSorter(SortSequence.Asc), x => x.LastModified.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern));
            FirePropertyChangedFilesAndFolders();
            IsSorting = false;
            SettingsService.Instance.LocalSettings.GroupMode = GroupMode.GroupByDateAscending;
        }

        public void GroupByDateDescending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new DateSorter(SortSequence.Desc), x => x.LastModified.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern));
            _groupedFolders.ArrangeItems(new DateSorter(SortSequence.Desc), x => x.LastModified.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern));
            FirePropertyChangedFilesAndFolders();
            IsSorting = false;
            SettingsService.Instance.LocalSettings.GroupMode = GroupMode.GroupByDateDescending;
        }

        public void GroupBySizeAscending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new SizeSorter(SortSequence.Asc), GetSizeHeader);
            _groupedFolders.ArrangeItems(new SizeSorter(SortSequence.Asc), GetSizeHeader);
            FirePropertyChangedFilesAndFolders();
            IsSorting = false;
            SettingsService.Instance.LocalSettings.GroupMode = GroupMode.GroupBySizeAscending;
        }

        public void GroupBySizeDescending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new SizeSorter(SortSequence.Desc), GetSizeHeader);
            _groupedFolders.ArrangeItems(new SizeSorter(SortSequence.Desc), GetSizeHeader);
            FirePropertyChangedFilesAndFolders();
            IsSorting = false;
            SettingsService.Instance.LocalSettings.GroupMode = GroupMode.GroupBySizeDescending;
        }

        public void GroupByTypeAscending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new NameSorter(SortSequence.Asc), x => x.ContentType);
            _groupedFolders.ArrangeItems(new NameSorter(SortSequence.Asc), x => x.ContentType);
            FirePropertyChangedFilesAndFolders();
            IsSorting = false;
            SettingsService.Instance.LocalSettings.GroupMode = GroupMode.GroupByTypeAscending;
        }

        public void GroupByTypeDescending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new NameSorter(SortSequence.Desc), x => x.ContentType);
            _groupedFolders.ArrangeItems(new NameSorter(SortSequence.Desc), x => x.ContentType);
            FirePropertyChangedFilesAndFolders();
            IsSorting = false;
            SettingsService.Instance.LocalSettings.GroupMode = GroupMode.GroupByTypeDescending;
        }

        public void ToggleSelectionMode()
        {
            IsSelecting = IsSelecting ? false : true;
        }

        private static string GetSizeHeader(ResourceInfo fileOrFolder)
        {
            var sizeMb = fileOrFolder.Size / 1024f / 1024f;

            long[] sizesValuesMb = { 1, 5, 10, 25, 50, 100, 250, 500, 1024, 5120, 10240, 102400, 1048576 };
            string[] sizesDisplay = { "<1MB", ">1MB", ">5MB", ">10MB", ">25MB", ">50MB", ">100MB", ">250MB", ">500MB", ">1GB", ">5GB", ">10GB", ">100GB", ">1TB" };

            var index = 0;

            for (int i = 0; i < sizesValuesMb.Length; i++)
            {
                if (sizeMb > sizesValuesMb[i])
                    index++;
                else
                    break;
            }

            return sizesDisplay[index];
        }

        public async Task Refresh()
        {
            await StartDirectoryListing();
        }

        public async Task StartDirectoryListing()
        {
            await StartDirectoryListing(null, null);
        }

        public async Task StartDirectoryListing(ResourceInfo resourceInfoToExclude, String viewName = null)
        {
            var client = await ClientService.GetClient();

            if (client == null || IsSelecting)
            {
                return;
            }

            _continueListing = true;

            if (PathStack.Count == 0)
            {
                PathStack.Add(new PathInfo
                {
                    ResourceInfo = new ResourceInfo()
                    {
                        Name = "Nextcloud",
                        Path = "/"
                    },
                    IsRoot = true
                });
            }

            var path = PathStack.Count > 0 ? PathStack[PathStack.Count - 1].ResourceInfo.Path : "/";
            List<ResourceInfo> list = null;

            try
            {
                if (viewName == "sharesIn" | viewName == "sharesOut" | viewName == "sharesLink")
                {
                    PathStack.Clear();
                    list = await client.GetSharesView(viewName);
                }
                else if (viewName == "favorites")
                {
                    PathStack.Clear();
                    list = await client.GetFavorites();
                }
                else
                {
                    list = await client.List(path);
                }
            }
            catch (ResponseError e)
            {
                ResponseErrorHandlerService.HandleException(e);
            }

            FilesAndFolders.Clear();
            Folders.Clear();

            if (list != null)
            {
                foreach (var item in list)
                {
                    if (resourceInfoToExclude != null && item == resourceInfoToExclude)
                        continue;

                    FilesAndFolders.Add(new FileOrFolder(item));

                    if (item.IsDirectory)
                    {
                        if (RemoveResourceInfos != null)
                        {
                            int index = RemoveResourceInfos.FindIndex(
                                delegate (ResourceInfo res)
                                {
                                    return res.Path.Equals(item.Path, StringComparison.Ordinal);
                                });
                            if (index == -1)
                            {
                                Folders.Add(new FileOrFolder(item));
                            }
                        }
                        else
                        {
                            Folders.Add(new FileOrFolder(item));
                        }
                    }
                }
            }

            switch (SettingsService.Instance.LocalSettings.PreviewImageDownloadMode)
            {
                case PreviewImageDownloadMode.Always:
                    DownloadPreviewImages();
                    break;
                case PreviewImageDownloadMode.WiFiOnly:
                    var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                    // connectionProfile can be null (e.g. airplane mode)
                    if (connectionProfile != null && connectionProfile.IsWlanConnectionProfile)
                    {
                        DownloadPreviewImages();
                    }
                    break;
                case PreviewImageDownloadMode.Never:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            SortList();
        }

        private async void DownloadPreviewImages()
        {
            var client = await ClientService.GetClient();
            if (client == null)
            {
                return;
            }

            foreach (var currentFile in FilesAndFolders.ToArray())
            {
                if (!_continueListing)
                {
                    break;
                }

                try
                {
                    Stream stream = null;
                    try
                    {
                        stream = await client.GetThumbnail(currentFile, 120, 120);
                    }
                    catch (ResponseError e)
                    {
                        ResponseErrorHandlerService.HandleException(e);
                    }

                    if (stream == null)
                    {
                        continue;
                    }
                    var bitmap = new BitmapImage();
                    using (var memStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memStream);
                        memStream.Position = 0;
                        bitmap.SetSource(memStream.AsRandomAccessStream());
                    }
                    currentFile.Thumbnail = bitmap;
                }
                catch (ResponseError)
                {
                    currentFile.Thumbnail = new BitmapImage
                    {
                        UriSource = new Uri("ms-appx:///Assets/Images/ThumbnailNotFound.png")
                    };
                }
            }
        }

        public bool IsSorting
        {
            get { return _isSorting; }
            set
            {
                if (_isSorting == value)
                {
                    return;
                }
                _isSorting = value;
                SelectionMode = _isSorting ? "None" : "Single";
                OnPropertyChanged();
            }
        }

        public string SelectionMode
        {
            get { return _selectionMode; }
            set
            {
                if (_selectionMode == value)
                {
                    return;
                }
                _selectionMode = value;
                OnPropertyChanged();
            }
        }

        public bool IsSelecting
        {
            get { return _isSelecting; }
            set
            {
                if (_isSelecting == value)
                {
                    return;
                }
                _isSelecting = value;
                SelectionMode = _isSelecting ? "Multiple" : "Single";
                OnPropertyChanged();
            }
        }

        public List<ResourceInfo> RemoveResourceInfos { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void StopDirectoryListing()
        {
            _continueListing = false;
        }

        public async Task<bool> CreateDirectory(string directoryName)
        {
            var client = await ClientService.GetClient();

            if (client == null)
            {
                return false;
            }

            var path = PathStack.Count > 0 ? PathStack[PathStack.Count - 1].ResourceInfo.Path : "";
            var success = false;

            try
            {
                success = await client.CreateDirectory(path + directoryName);
            }
            catch (ResponseError e)
            {
                if (e.StatusCode != "400") // ProtocolError
                {
                    ResponseErrorHandlerService.HandleException(e);
                }
            }

            if (success)
            {
                await StartDirectoryListing();
            }

            return success;
        }

        public async Task<bool> DeleteResource(ResourceInfo resourceInfo)
        {
            var client = await ClientService.GetClient();

            if (client == null)
            {
                return false;
            }

            var path = resourceInfo.ContentType.Equals("dav/directory")
                ? resourceInfo.Path
                : resourceInfo.Path + "/" + resourceInfo.Name;

            var success = await client.Delete(path);
            await StartDirectoryListing();
            return success;
        }

        public async Task<bool> DeleteSelected(List<ResourceInfo> resourceInfos)
        {
            var client = await ClientService.GetClient();

            if (client == null)
            {
                return false;
            }

            foreach (var resourceInfo in resourceInfos)
            {
                var path = resourceInfo.ContentType.Equals("dav/directory")
                ? resourceInfo.Path
                : resourceInfo.Path + "/" + resourceInfo.Name;
                var success = await client.Delete(path);
                if (!success)
                {
                    return success;
                }
            }

            await StartDirectoryListing();
            return true;
        }

        public async Task<bool> Rename(string oldName, string newName)
        {
            var client = await ClientService.GetClient();

            if (client == null)
            {
                return false;
            }

            var path = PathStack.Count > 0 ? PathStack[PathStack.Count - 1].ResourceInfo.Path : "";
            var success = false;

            try
            {
                success = await client.Move(path + oldName, path + newName);
            }
            catch (ResponseError e)
            {
                if (e.StatusCode != "400") // ProtocolError
                {
                    ResponseErrorHandlerService.HandleException(e);
                }
            }

            if (success)
            {
                await StartDirectoryListing();
            }
            return success;
        }

        public async Task<bool> Move(string oldPath, string newPath)
        {
            var client = await ClientService.GetClient();

            if (client == null)
            {
                return false;
            }

            var success = false;

            try
            {
                success = await client.Move(oldPath, newPath);
            }
            catch (ResponseError e)
            {
                if (e.StatusCode != "400") // ProtocolError
                {
                    ResponseErrorHandlerService.HandleException(e);
                }
            }

            if (success)
            {
                await StartDirectoryListing();
            }
            return success;
        }
    }
}
