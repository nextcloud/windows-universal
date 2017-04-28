using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using Windows.Networking.Connectivity;
using Windows.Storage;
using NextcloudApp.Converter;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudApp.Utils;
using NextcloudClient.Exceptions;
using NextcloudClient.Types;
using Prism.Commands;
using Prism.Events;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;

namespace NextcloudApp.ViewModels
{
    public class MenuViewModel : ViewModel
    {
        private const string CurrentPageTokenKey = "CurrentPageToken";
        private readonly Dictionary<PageToken, bool> _canNavigateLookup;
        private PageToken _currentPageToken;
        private readonly INavigationService _navigationService;
        private readonly ISessionStateService _sessionStateService;
        private bool _showMenuButton;
        private bool _isMenuOpen;
        private User _user;
        private Uri _userAvatarUrl;
        private string _quotaUsedOfTotalString;

        public MenuViewModel(IEventAggregator eventAggregator, INavigationService navigationService, IResourceLoader resourceLoader, ISessionStateService sessionStateService)
        {
            eventAggregator.GetEvent<NavigationStateChangedEvent>().Subscribe(OnNavigationStateChanged);
            _navigationService = navigationService;
            _sessionStateService = sessionStateService;

            Commands = new ObservableCollection<MenuItem>
            {
                new MenuItem
                {
                    DisplayName = resourceLoader.GetString("AllFiles"),
                    FontIcon = "\uE8B7",
                    Command = new DelegateCommand(
                        () => NavigateToPage(PageToken.DirectoryList),
                        () => CanNavigateToPage(PageToken.DirectoryList)
                    )
                },
                new MenuItem
                {
                    DisplayName = resourceLoader.GetString("Favorites"),
                    FontIcon = "\uE734",
                    Command = new DelegateCommand(
                        () => NavigateToPage(PageToken.Favorites),
                        () => CanNavigateToPage(PageToken.Favorites)
                    )
                },
                new MenuItem
                {
                    DisplayName = resourceLoader.GetString("SharingIn"),
                    FontIcon = "\uF003",
                    Command = new DelegateCommand(
                        () => NavigateToPage(PageToken.SharesIn),
                        () => CanNavigateToPage(PageToken.SharesIn)
                    )
                },
                new MenuItem
                {
                    DisplayName = resourceLoader.GetString("SharingOut"),
                    FontIcon = "\uF003",
                    Command = new DelegateCommand(
                        () => NavigateToPage(PageToken.SharesOut),
                        () => CanNavigateToPage(PageToken.SharesOut)
                    )
                },
                //new MenuItem
                //{
                //    DisplayName = resourceLoader.GetString("SharingLink"),
                //    FontIcon = "\uE167",
                //    Command = new DelegateCommand(
                //        () => NavigateToPage(PageToken.SharesLink),
                //        () => CanNavigateToPage(PageToken.SharesLink)
                //    )
                //},
                
            };

            ExtraCommands = new ObservableCollection<MenuItem>
            {
                 new MenuItem
                {
                    DisplayName = resourceLoader.GetString("SynchronizationStatus/Header"),
                    FontIcon = "\uE895",
                    Command = new DelegateCommand(
                        () => NavigateToPage(PageToken.SyncStatus),
                        () => CanNavigateToPage(PageToken.SyncStatus)
                    )
                },
                new MenuItem
                {
                    DisplayName = resourceLoader.GetString("Settings"),
                    FontIcon = "\uE713",
                    Command = new DelegateCommand(
                        () => NavigateToPage(PageToken.Settings),
                        () => CanNavigateToPage(PageToken.Settings)
                    )
                },               
            };
            
            SettingsService.Instance.LocalSettings.PropertyChanged += (sender, args) =>
            {
                GetUserInformation();
            };
            GetUserInformation();

            _canNavigateLookup = new Dictionary<PageToken, bool>();

            foreach (PageToken pageToken in Enum.GetValues(typeof(PageToken)))
            {
                _canNavigateLookup.Add(pageToken, true);
            }

            if (!_sessionStateService.SessionState.ContainsKey(CurrentPageTokenKey))
            {
                return;
            }
            // Resuming, so update the menu to reflect the current page correctly
            PageToken currentPageToken;
            if (!Enum.TryParse(_sessionStateService.SessionState[CurrentPageTokenKey].ToString(), out currentPageToken))
            {
                return;
            }
            UpdateCanNavigateLookup(currentPageToken);
            RaiseCanExecuteChanged();
        }

        private async void GetUserInformation()
        {
            var client = await ClientService.GetClient();
            if (client == null)
            {
                return;
            }

            var username = SettingsService.Instance.LocalSettings.Username;

            if (string.IsNullOrEmpty(username))
                return;

            try { 
                User = await client.GetUserAttributes(username);

                var converter = new BytesToHumanReadableConverter();
                QuotaUsedOfTotalString = LocalizationService.Instance.GetString(
                    "QuotaUsedOfTotal",
                    converter.Convert(User.Quota.Used, typeof(string), null, CultureInfo.CurrentCulture.ToString()),
                    converter.Convert(User.Quota.Total, typeof(string), null, CultureInfo.CurrentCulture.ToString())
                );

                switch (SettingsService.Instance.LocalSettings.PreviewImageDownloadMode)
                {
                    case PreviewImageDownloadMode.Always:
                        UserAvatarUrl = await client.GetUserAvatarUrl(username, 120);
                        break;
                    case PreviewImageDownloadMode.WiFiOnly:
                        var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                        // connectionProfile can be null (e.g. airplane mode)
                        if (connectionProfile != null && connectionProfile.IsWlanConnectionProfile)
                        {
                            UserAvatarUrl = await client.GetUserAvatarUrl(username, 120);
                        }
                        break;
                    case PreviewImageDownloadMode.Never:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (ResponseError e)
            {
                ResponseErrorHandlerService.HandleException(e);
            }
        }

        public string QuotaUsedOfTotalString
        {
            get { return _quotaUsedOfTotalString; }
            set { SetProperty(ref _quotaUsedOfTotalString, value); }
        }

        public User User
        {
            get { return _user; }
            set { SetProperty(ref _user, value); }
        }

        public Uri UserAvatarUrl
        {
            get { return _userAvatarUrl; }
            set { SetProperty(ref _userAvatarUrl, value); }
        }
        
        public ObservableCollection<MenuItem> Commands { get; set; }

        public ObservableCollection<MenuItem> ExtraCommands { get; set; }

        private void OnNavigationStateChanged(NavigationStateChangedEventArgs args)
        {
            PageToken currentPageToken;
            if (!Enum.TryParse(args.Sender.Content.GetType().Name.Replace("Page", string.Empty), out currentPageToken))
            {
                return;
            }
            _sessionStateService.SessionState[CurrentPageTokenKey] = currentPageToken.ToString();
            UpdateCanNavigateLookup(currentPageToken);
            RaiseCanExecuteChanged();
        }

        private void NavigateToPage(PageToken pageToken)
        {
            if (!CanNavigateToPage(pageToken))
            {
                return;
            }
            if (!_navigationService.Navigate(pageToken.ToString(), null))
            {
                return;
            }
            UpdateCanNavigateLookup(pageToken);
            RaiseCanExecuteChanged();
        }

        private bool CanNavigateToPage(PageToken pageToken)
        {
            return _canNavigateLookup[pageToken];
        }

        private void UpdateCanNavigateLookup(PageToken navigatedTo)
        {
            if (navigatedTo == _currentPageToken)
            {
                return;
            }
            _canNavigateLookup[_currentPageToken] = true;
            _canNavigateLookup[navigatedTo] = false;
            _currentPageToken = navigatedTo;
            ShowMenuButton = 
                _currentPageToken != PageToken.Login &&
                _currentPageToken != PageToken.FileDownload &&
                _currentPageToken != PageToken.FileUpload;
            IsMenuOpen = false;
        }

        public bool ShowMenuButton
        {
            get { return _showMenuButton; }
            set { SetProperty(ref _showMenuButton, value); }
        }

        public bool IsMenuOpen
        {
            get { return _isMenuOpen; }
            set { SetProperty(ref _isMenuOpen, value); }
        }

        private void RaiseCanExecuteChanged()
        {
            foreach (var item in Commands)
            {
                var delegateCommand = item.Command as DelegateCommand;
                delegateCommand?.RaiseCanExecuteChanged();
            }
            foreach (var item in ExtraCommands)
            {
                var delegateCommand = item.Command as DelegateCommand;
                delegateCommand?.RaiseCanExecuteChanged();
            }
        }
    }
}