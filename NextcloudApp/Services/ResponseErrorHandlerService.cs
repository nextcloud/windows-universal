using System.Net;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Practices.Unity;
using NextcloudClient.Exceptions;
using Prism.Windows.AppModel;
using Prism.Windows.Navigation;

namespace NextcloudApp.Services
{
    public class ResponseErrorHandlerService
    {
        private static bool _isHandlingException;

        public static async void HandleException(ResponseError e)
        {
            if (_isHandlingException)
            {
                return;
            }
            _isHandlingException = true;
            var app = Application.Current as App;
            if (app == null)
            {
                return;
            }
            var resourceLoader = app.Container.Resolve<IResourceLoader>();
            var dialogService = app.Container.Resolve<DialogService>();
            var navigationService = app.Container.Resolve<INavigationService>();

            ContentDialog dialog;
            if (e.StatusCode == "401")
            {
                dialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("NotAuthorized"),
                    Content = new TextBlock
                    {
                        Text = resourceLoader.GetString("NotAuthorized_Description"),
                        TextWrapping = TextWrapping.WrapWholeWords,
                        Margin = new Thickness(0, 20, 0, 0)
                    },
                    PrimaryButtonText = resourceLoader.GetString("OK")
                };
                await dialogService.ShowAsync(dialog);
                navigationService.Navigate(PageToken.Login.ToString(), null);
                return;
            }
            if (e.StatusCode == "503") // Maintenance mode
            {
                dialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("AnErrorHasOccurred"),
                    Content = new TextBlock
                    {
                        Text = resourceLoader.GetString("ServiceTemporarilyUnavailable"),
                        TextWrapping = TextWrapping.WrapWholeWords,
                        Margin = new Thickness(0, 20, 0, 0)
                    },
                    PrimaryButtonText = resourceLoader.GetString("OK")
                };
                await dialogService.ShowAsync(dialog);
                navigationService.Navigate(PageToken.Login.ToString(), null);
                app.Exit();
                return;
            }
            dialog = new ContentDialog
            {
                Title = resourceLoader.GetString("AnErrorHasOccurred"),
                Content = new TextBlock
                {
                    Text = e.Message,
                    TextWrapping = TextWrapping.WrapWholeWords,
                    Margin = new Thickness(0, 20, 0, 0)
                },
                PrimaryButtonText = resourceLoader.GetString("OK")
            };
            await dialogService.ShowAsync(dialog);

            _isHandlingException = false;
        }
    }
}
