using System;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;

namespace NextcloudApp.Services
{
    public class StatusBarService
    {
        private static StatusBarService _instance;
        private int _waitCounter;

        public static StatusBarService Instance => _instance ?? (_instance = new StatusBarService());

        public async void ShowProgressIndicator()
        {
            _waitCounter++;

            if (!ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                return;
            }
            var statusBar = StatusBar.GetForCurrentView();

            var asyncAction = statusBar?.ProgressIndicator.ShowAsync();
            if (asyncAction != null)
            {
                await asyncAction;
            }
        }

        public async void HideProgressIndicator()
        {
            _waitCounter--;
            if (_waitCounter > 0)
            {
                return;
            }

            if (!ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                return;
            }
            var statusBar = StatusBar.GetForCurrentView();
            var asyncAction = statusBar?.ProgressIndicator.HideAsync();
            if (asyncAction != null)
            {
                await asyncAction;
            }
        }
    }
}
