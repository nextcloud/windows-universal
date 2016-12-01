using Windows.Security.Credentials;

namespace NextcloudApp.Services
{
    public static class ClientService
    {
        private static NextcloudClient.NextcloudClient _client;

        public static NextcloudClient.NextcloudClient GetClient()
        {
            if (_client != null)
            {
                return _client;
            }

            if (!string.IsNullOrEmpty(SettingsService.Instance.LocalSettings.ServerAddress) &&
                !string.IsNullOrEmpty(SettingsService.Instance.LocalSettings.Username))
            {
                var vault = new PasswordVault();
                PasswordCredential credentials = null;

                try
                {
                    credentials = vault.Retrieve(
                        SettingsService.Instance.LocalSettings.ServerAddress,
                        SettingsService.Instance.LocalSettings.Username
                    );
                }
                catch
                {
                }

                if (credentials != null)
                {
                    _client = new NextcloudClient.NextcloudClient(
                        credentials.Resource,
                        credentials.UserName,
                        credentials.Password
                        );
                }
            }

            SettingsService.Instance.LocalSettings.PropertyChanged += (sender, args) =>
            {
                if (
                    string.IsNullOrEmpty(SettingsService.Instance.LocalSettings.ServerAddress) ||
                    string.IsNullOrEmpty(SettingsService.Instance.LocalSettings.Username)
                    )
                {
                    return;
                }

                var vault = new PasswordVault();
                PasswordCredential credentials = null;

                try
                {
                    credentials = vault.Retrieve(
                        SettingsService.Instance.LocalSettings.ServerAddress,
                        SettingsService.Instance.LocalSettings.Username
                    );
                }
                catch
                {
                }

                if (credentials == null)
                {
                    return;
                }

                _client = new NextcloudClient.NextcloudClient(
                    credentials.Resource,
                    credentials.UserName,
                    credentials.Password
                    );
            };

            return _client;
        }
    }
}
