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
            GroupByNameAscending();
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

        public ObservableCollection<Grouping<string, FileOrFolder>> GroupedFilesAndFolders => _groupedFilesAndFolders.Items;
        public ObservableCollection<Grouping<string, FileOrFolder>> GroupedFolders => _groupedFolders.Items;

        public void GroupByNameAscending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new NameSorter(SortMode.Asc), x => x.Name.First().ToString().ToUpper());
            _groupedFolders.ArrangeItems(new NameSorter(SortMode.Asc), x => x.Name.First().ToString().ToUpper());
            OnPropertyChanged(nameof(GroupedFilesAndFolders));
            OnPropertyChanged(nameof(GroupedFolders));
            IsSorting = false;
        }

        public void GroupByNameDescending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new NameSorter(SortMode.Desc), x => x.Name.First().ToString().ToUpper());
            _groupedFolders.ArrangeItems(new NameSorter(SortMode.Desc), x => x.Name.First().ToString().ToUpper());
            OnPropertyChanged(nameof(GroupedFilesAndFolders));
            OnPropertyChanged(nameof(GroupedFolders));
            IsSorting = false;
        }

        public void GroupByDateAscending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new DateSorter(SortMode.Asc), x => x.LastModified.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern));
            _groupedFolders.ArrangeItems(new DateSorter(SortMode.Asc), x => x.LastModified.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern));
            OnPropertyChanged(nameof(GroupedFilesAndFolders));
            OnPropertyChanged(nameof(GroupedFolders));
            IsSorting = false;
        }

        public void GroupByDateDescending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new DateSorter(SortMode.Desc), x => x.LastModified.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern));
            _groupedFolders.ArrangeItems(new DateSorter(SortMode.Desc), x => x.LastModified.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern));
            OnPropertyChanged(nameof(GroupedFilesAndFolders));
            OnPropertyChanged(nameof(GroupedFolders));
            IsSorting = false;
        }

        public void GroupBySizeAscending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new SizeSorter(SortMode.Asc), GetSizeHeader);
            _groupedFolders.ArrangeItems(new SizeSorter(SortMode.Asc), GetSizeHeader);
            OnPropertyChanged(nameof(GroupedFilesAndFolders));
            OnPropertyChanged(nameof(GroupedFolders));
            IsSorting = false;
        }

        public void GroupBySizeDescending()
        {
            IsSorting = true;
            _groupedFilesAndFolders.ArrangeItems(new SizeSorter(SortMode.Desc), GetSizeHeader);
            _groupedFolders.ArrangeItems(new SizeSorter(SortMode.Desc), GetSizeHeader);
            OnPropertyChanged(nameof(GroupedFilesAndFolders));
            OnPropertyChanged(nameof(GroupedFolders));
            IsSorting = false;
        }

        private static string GetSizeHeader(ResourceInfo fileOrFolder)
        {
            var size = fileOrFolder.Size;
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            var order = 0;
            while (size >= 1024 && ++order < sizes.Length)
            {
                size = size / 1024;
            }
            return sizes[order];
        }

        public async Task Refresh()
        {
            await StartDirectoryListing();
        }

        public async Task StartDirectoryListing()
        {
            /*
             * Also contains webdav session after client has been returned
             * */
            var client = await ClientService.GetClient();
            if (client == null)
            {
                return;
            }

            var remoteShares = await client.ListOpenRemoteShare();
            var shares = await client.GetShares("");
            //List<ResourceInfo> sharesList = null;
            List<ResourceInfo> sharesList = new List<ResourceInfo>();


            //try
            //{
            //    sharesList = await client.List(shares[0].TargetPath);
            //}
            //catch (ResponseError e)
            //{
            //    ResponseErrorHandlerService.HandleException(e);
            //}
            ResourceInfo resource;
            resource = await client.GetResourceInfo(shares[1].Path);
            //sharesList = await client.List(shares[1].Path);

            try
            {
                sharesList.Add(resource);
            }
            catch (ResponseError e)
            {
                ResponseErrorHandlerService.HandleException(e);
            }

            //foreach (var item in shares)
            //{
            //    try
            //    {
            //        resource = await client.GetResourceInfo(item.TargetPath);
            //        sharesList.Add(resource);
            //    }
            //    catch (ResponseError e)
            //    {
            //        ResponseErrorHandlerService.HandleException(e);
            //    }
            //}



            _continueListing = true;

            var path = PathStack.Count > 0 ? PathStack[PathStack.Count - 1].ResourceInfo.Path : "/";
            List<ResourceInfo> list = null;

            try
            {
                list = await client.List(path);
            }
            catch (ResponseError e)
            {
                ResponseErrorHandlerService.HandleException(e);
            }
            list = sharesList;
            FilesAndFolders.Clear();
            Folders.Clear();
            if (list != null)
            {
                foreach (var item in list)
                {
                    FilesAndFolders.Add(new FileOrFolder(item));
                    if (item.IsDirectory())
                    {
                        Folders.Add(new FileOrFolder(item));
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
                OnPropertyChanged();
            }
        }

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
