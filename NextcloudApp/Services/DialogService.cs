using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace NextcloudApp.Services
{
    public class DialogService
    {
        private static SemaphoreSlim _semaphore;

        public DialogService()
        {
            _semaphore = new SemaphoreSlim(1);
        }

        public async Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
        {
            await _semaphore.WaitAsync();
            var result = await dialog.ShowAsync();
            _semaphore.Release();
            return result;
        }

        public async Task<IUICommand> ShowAsync(MessageDialog dialog)
        {
            await _semaphore.WaitAsync();
            var result = await dialog.ShowAsync();
            _semaphore.Release();
            return result;
        }
    }
}
