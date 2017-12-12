using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Practices.Unity;
using NextcloudApp.Services;
using Prism.Unity.Windows;
using Prism.Windows.AppModel;
using System.Collections.Generic;
using Prism.Windows.Navigation;
using NextcloudApp.Controls;
using System.Diagnostics;
using Windows.UI.Xaml.Media;
using System.ComponentModel;
using NextcloudApp.Annotations;
using System.Runtime.CompilerServices;

namespace NextcloudApp
{
    public partial class AppShell : ThemeablePage, INotifyPropertyChanged
    {
        private const string CurrentPageTokenKey = "CurrentPageToken";
        private readonly Dictionary<PageToken, bool> _canNavigateLookup = new Dictionary<PageToken, bool>();
        private PageToken _currentPageToken;
        private bool _pathStackHeaderVisible = true;
        private bool _settingsHeaderVisible = false;
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
            frame.Navigated += Frame_Navigated;
            NavView.Content = frame;
        }

        private void Frame_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            
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
            foreach (PageToken pageToken in System.Enum.GetValues(typeof(PageToken)))
            {
                _canNavigateLookup.Add(pageToken, true);
            }
            

            //NavView.MenuItems.Add(new NavigationViewItemSeparator());
            //< NavigationViewItemHeader Content = "Main pages" />


            NavView.MenuItems.Add(new NavigationViewItem
            {
                Content = new Models.MenuItem
                {
                    DisplayName = _resourceLoader.GetString("AllFiles"),
                    PageToken = PageToken.DirectoryList
                },
                Icon = new FontIcon { Glyph = "\uE8B7" }
            });

            NavView.MenuItems.Add(new NavigationViewItem
            {
                Content = new Models.MenuItem
                {
                    DisplayName = _resourceLoader.GetString("Favorites"),
                    PageToken = PageToken.Favorites
                },
                Icon = new FontIcon { Glyph = "\uE734" }
            });

            NavView.MenuItems.Add(new NavigationViewItem
            {
                Content = new Models.MenuItem
                {
                    DisplayName = _resourceLoader.GetString("SharingIn"),
                    PageToken = PageToken.SharesIn
                },
                Icon = new FontIcon { Glyph = "\uF003" }
            });

            NavView.MenuItems.Add(new NavigationViewItem
            {
                Content = new Models.MenuItem
                {
                    DisplayName = _resourceLoader.GetString("SharingOut"),
                    PageToken = PageToken.SharesOut
                },
                Icon = new FontIcon { Glyph = "\uF003" }
            });

            NavView.MenuItems.Add(new NavigationViewItem
            {
                Content = new Models.MenuItem
                {
                    DisplayName = _resourceLoader.GetString("SharingLink"),
                    PageToken = PageToken.SharesLink
                },
                Icon = new FontIcon { Glyph = "\uE167" }
            });

            foreach (var item in NavView.MenuItems)
            {
                var navViewItem = item as NavigationViewItem;
                if (
                    navViewItem == null ||
                    (navViewItem.Content as Models.MenuItem).PageToken != PageToken.DirectoryList
                )
                {
                    continue;
                }
                NavView.SelectedItem = item;
                break;
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
        }

        public bool PathStackHeaderVisible {
            get => _pathStackHeaderVisible;
            internal set
            {
                _pathStackHeaderVisible = value;
                OnPropertyChanged();
            }
        }
        public bool SettingsHeaderVisible {
            get => _settingsHeaderVisible;
            internal set
            {
                _settingsHeaderVisible = value;
                OnPropertyChanged();
            }
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                PathStackHeaderVisible = false;
                SettingsHeaderVisible = true;
                NavigateToPage(PageToken.Settings);
            }
            else
            {
                PathStackHeaderVisible = true;
                SettingsHeaderVisible = false;
                NavigateToPage((args.InvokedItem as Models.MenuItem).PageToken);
            }
        }

        /// <summary>
        /// Extension method for a FrameworkElement that searches for a child element by type and name.
        /// </summary>
        /// <typeparam name="T">The type of the child element to search for.</typeparam>
        /// <param name="element">The parent framework element.</param>
        /// <param name="sChildName">The name of the child element to search for.</param>
        /// <returns>The matching child element, or null if none found.</returns>
        public static T FindElementByName<T>(FrameworkElement element, string sChildName) where T : FrameworkElement
        {
            Debug.WriteLine("[FindElementByName] ==> element [{0}] sChildName [{1}] T [{2}]", element, sChildName, typeof(T).ToString());

            T childElement = null;

            //
            // Spin through immediate children of the starting element.
            //
            var nChildCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < nChildCount; i++)
            {
                // Get next child element.
                FrameworkElement child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                Debug.WriteLine("Found child [{0}]", child);

                // Do we have a child?
                if (child == null)
                    continue;

                // Is child of desired type and name?
                if (child is T && child.Name.Equals(sChildName))
                {
                    // Bingo! We found a match.
                    childElement = (T)child;
                    Debug.WriteLine("Found matching element [{0}]", childElement);
                    break;
                } // if

                // Recurse and search through this child's descendants.
                childElement = FindElementByName<T>(child, sChildName);

                // Did we find a matching child?
                if (childElement != null)
                    break;
            } // for

            Debug.WriteLine("[FindElementByName] <== childElement [{0}]", childElement);
            return childElement;
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
