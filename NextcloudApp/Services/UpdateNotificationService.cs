using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Windows.AppModel;

namespace NextcloudApp.Services
{
    public static class UpdateNotificationService
    {
        /// <summary>
        /// Notifies the user.
        /// </summary>
        public static void NotifyUser(Grid updateDialogContainer, ContentControl updateDialogTitle, TextBlock updateDialogContent, Button updateDialogButton1, Button updateDialogButton2)
        {
            SettingsService.Default.Value.LocalSettings.ShowUpdateMessage = false;

            var app = Application.Current as App;
            if (app == null)
            {
                return;
            }
            var resourceLoader = app.Container.Resolve<IResourceLoader>();
            //var dialogService = app.Container.Resolve<DialogService>();

            var currentVersion =
                $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";

            var currentVersionWithDash =
                $"{Package.Current.Id.Version.Major}-{Package.Current.Id.Version.Minor}-{Package.Current.Id.Version.Build}";

            var changelog = resourceLoader.GetString(string.Format("Changes_version_{0}", currentVersionWithDash));

            if (string.IsNullOrEmpty(changelog))
            {
                return;
            }

            var line1 = string.Format(resourceLoader.GetString("Changes_Intro_Line_1"), currentVersion);
            var line2 = resourceLoader.GetString("Changes_Intro_Line_2");
            
            /*
            var dialog = new ContentDialog
            {
                Title = resourceLoader.GetString("Changes_Title"),
                Content = new ScrollViewer
                {
                    HorizontalScrollMode = ScrollMode.Disabled,
                    VerticalScrollMode = ScrollMode.Auto,
                    Content = new TextBlock
                    {
                        Text = string.Format("{0}\n\n{1}\n\n{2}", line1, line2, changelog),
                        TextWrapping = TextWrapping.WrapWholeWords,
                        Margin = new Thickness(0, 20, 0, 0)
                    }
                },
                PrimaryButtonText = resourceLoader.GetString("OK"),
                SecondaryButtonText = resourceLoader.GetString("Changes_Recommend"),
                SecondaryButtonCommand = new DelegateCommand(RecommendChanges)
            };
            await dialogService.ShowAsync(dialog);
            */

            updateDialogTitle.Content = resourceLoader.GetString("Changes_Title");
            updateDialogButton1.Content = resourceLoader.GetString("OK");
            updateDialogButton1.Command = new DelegateCommand(() =>
            {
                updateDialogContainer.Visibility = Visibility.Collapsed;
            });
            updateDialogButton2.Content = resourceLoader.GetString("Changes_Recommend");
            updateDialogButton2.Command = new DelegateCommand(RecommendChanges);
            updateDialogContent.Text = string.Format("{0}\n\n{1}\n\n{2}", line1, line2, changelog);
            updateDialogContainer.Visibility = Visibility.Visible;
        }

        private static void RecommendChanges()
        {
            var dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += delegate(DataTransferManager sender, DataRequestedEventArgs args)
            {
                var app = Application.Current as App;
                if (app == null)
                {
                    return;
                }
                var resourceLoader = app.Container.Resolve<IResourceLoader>();

                var currentVersionWithDash =
                    $"{Package.Current.Id.Version.Major}-{Package.Current.Id.Version.Minor}-{Package.Current.Id.Version.Build}";

                var line2 = resourceLoader.GetString("Changes_Intro_Line_2");
                var changelog = resourceLoader.GetString(string.Format("Changes_version_{0}", currentVersionWithDash));
                
                args.Request.Data.Properties.Title = resourceLoader.GetString("Changes_AppName");
                args.Request.Data.SetText(string.Format("{0}\n\n{1}\n\n{2}\n\n", line2, changelog, "https://www.microsoft.com/store/apps/9nblggh532xq"));
            };

            DataTransferManager.ShowShareUI();
        }
    }
}
