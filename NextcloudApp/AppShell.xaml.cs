using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using NextcloudApp.Services;

namespace NextcloudApp
{
    public sealed partial class AppShell
    {
        public AppShell()
        {
            InitializeComponent();
            ShowUpdateMessage();
        }

        private async void ShowUpdateMessage()
        {
            if (SettingsService.Instance.LocalSettings.ShowUpdateMessage)
            {
                await UpdateNotificationService.NotifyUser();
            }
            else
            {
                SettingsService.Instance.LocalSettings.PropertyChanged += async (sender, args) =>
                {
                    if (args.PropertyName.Equals("ShowUpdateMessage") && SettingsService.Instance.LocalSettings.ShowUpdateMessage)
                    {
                        await UpdateNotificationService.NotifyUser();
                    }
                };
            }
        }

        public void SetContentFrame(Frame frame)
        {
            RootSplitView.Content = frame;
        }

        public void SetMenuPaneContent(UIElement content)
        {
            RootSplitView.Pane = content;
        }

        public UIElement GetContentFrame()
        {
            return RootSplitView.Content;
        }
    }
}
