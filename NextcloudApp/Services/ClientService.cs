using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextcloudApp.Services
{
    public class ClientService
    {
        private static NextcloudClient.NextcloudClient _client;

        public static NextcloudClient.NextcloudClient GetClient()
        {
            if (_client != null)
            {
                return _client;
            }

            if (!string.IsNullOrEmpty(SettingsService.Instance.Settings.ServerAddress) &&
                !string.IsNullOrEmpty(SettingsService.Instance.Settings.Username) &&
                !string.IsNullOrEmpty(SettingsService.Instance.Settings.Password))
            {
                _client = new NextcloudClient.NextcloudClient(
                    SettingsService.Instance.Settings.ServerAddress,
                    SettingsService.Instance.Settings.Username,
                    SettingsService.Instance.Settings.Password
                    );
            }

            SettingsService.Instance.Settings.PropertyChanged += (sender, args) =>
            {
                if (
                    string.IsNullOrEmpty(SettingsService.Instance.Settings.ServerAddress) ||
                    string.IsNullOrEmpty(SettingsService.Instance.Settings.Username) ||
                    string.IsNullOrEmpty(SettingsService.Instance.Settings.Password)
                    )
                {
                    return;
                }
                _client = new NextcloudClient.NextcloudClient(
                    SettingsService.Instance.Settings.ServerAddress,
                    SettingsService.Instance.Settings.Username,
                    SettingsService.Instance.Settings.Password
                    );
            };

            return _client;
        }
    }
}
