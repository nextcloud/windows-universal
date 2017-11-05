using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Practices.Unity;
using NextcloudApp.Services;
using Prism.Unity.Windows;
using Prism.Windows.AppModel;
using System.Collections.Generic;
using Prism.Windows.Navigation;

namespace NextcloudApp
{
    public sealed partial class AppShell
    {
        private const string CurrentPageTokenKey = "CurrentPageToken";
        private readonly Dictionary<PageToken, bool> _canNavigateLookup = new Dictionary<PageToken, bool>();
        private PageToken _currentPageToken;
        private readonly INavigationService _navigationService;
        private readonly ISessionStateService _sessionStateService;
        private readonly IResourceLoader _resourceLoader;

        public AppShell()
        {
            InitializeComponent();

            if (DesignMode.DesignModeEnabled) return;
            if (PrismUnityApplication.Current is App app)
            {
                _navigationService = app.Container.Resolve<INavigationService>();
                _sessionStateService = app.Container.Resolve<ISessionStateService>();
                _resourceLoader = app.Container.Resolve<IResourceLoader>();
            }
            ShowUpdateMessage();
        }

        private void ShowUpdateMessage()
        {
            if (SettingsService.Default.Value.LocalSettings.ShowUpdateMessage)
            {
                //UpdateNotificationService.NotifyUser(UpdateDialogContainer, UpdateDialogTitle, UpdateDialogContent, UpdateDialogButton1, UpdateDialogButton2);
            }
            else
            {
                SettingsService.Default.Value.LocalSettings.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName.Equals("ShowUpdateMessage") && SettingsService.Default.Value.LocalSettings.ShowUpdateMessage)
                    {
                        //UpdateNotificationService.NotifyUser(UpdateDialogContainer, UpdateDialogTitle, UpdateDialogContent, UpdateDialogButton1, UpdateDialogButton2);
                    }
                };
            }
        }

        public void SetContentFrame(Frame frame)
        {
            NavView.Content = frame;
        }

        public void SetMenuPaneContent(UIElement content)
        {
           // RootSplitView.Pane = content;
        }

        public UIElement GetContentFrame()
        {
            return (UIElement) NavView.Content;
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            //NavView.MenuItems.Add(new NavigationViewItemSeparator());
            //< NavigationViewItemHeader Content = "Main pages" />
            NavView.MenuItems.Add(new NavigationViewItem
            {
                Content = _resourceLoader.GetString("AllFiles"),
                Icon = new FontIcon { Glyph = "\uE8B7" },
                Tag = "AllFiles"
            });


            foreach (PageToken pageToken in System.Enum.GetValues(typeof(PageToken)))
            {
                _canNavigateLookup.Add(pageToken, true);
            }

            
            //new MenuItem
            //{
            //    DisplayName = resourceLoader.GetString("AllFiles"),
            //    FontIcon = "\uE8B7",
            //    Command = new DelegateCommand(
            //        () => NavigateToPage(PageToken.DirectoryList),
            //        () => CanNavigateToPage(PageToken.DirectoryList)
            //    )
            //},
            //new MenuItem
            //{
            //    DisplayName = resourceLoader.GetString("Favorites"),
            //    FontIcon = "\uE734",
            //    Command = new DelegateCommand(
            //        () => NavigateToPage(PageToken.Favorites),
            //        () => CanNavigateToPage(PageToken.Favorites)
            //    )
            //},
            //new MenuItem
            //{
            //    DisplayName = resourceLoader.GetString("SharingIn"),
            //    FontIcon = "\uF003",
            //    Command = new DelegateCommand(
            //        () => NavigateToPage(PageToken.SharesIn),
            //        () => CanNavigateToPage(PageToken.SharesIn)
            //    )
            //},
            //new MenuItem
            //{
            //    DisplayName = resourceLoader.GetString("SharingOut"),
            //    FontIcon = "\uF003",
            //    Command = new DelegateCommand(
            //        () => NavigateToPage(PageToken.SharesOut),
            //        () => CanNavigateToPage(PageToken.SharesOut)
            //    )
            //},
            //new MenuItem
            //{
            //    DisplayName = resourceLoader.GetString("SharingLink"),
            //    FontIcon = "\uE167",
            //    Command = new DelegateCommand(
            //        () => NavigateToPage(PageToken.SharesLink),
            //        () => CanNavigateToPage(PageToken.SharesLink)
            //    )
            //}

            foreach (NavigationViewItem item in NavView.MenuItems)
            {
                if (item.Tag.ToString() != "AllFiles") continue;
                NavView.SelectedItem = item;
                break;
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                NavigateToPage(PageToken.Settings);
            }
            else
            {
                switch ((args.InvokedItem as NavigationViewItem).Tag)
                {
                    case "AllFiles":
                        NavigateToPage(PageToken.DirectoryList);
                        break;
                }
            }
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
            //RaiseCanExecuteChanged();
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
            //ShowMenuButton =
            //    _currentPageToken != PageToken.Login &&
            //    _currentPageToken != PageToken.FileDownload &&
            //    _currentPageToken != PageToken.FileUpload;
            //IsMenuOpen = false;
        }
    }
}
