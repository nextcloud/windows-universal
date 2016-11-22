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

            if (!string.IsNullOrEmpty(SettingsService.Instance.Settings.ServerAddress) &&
                !string.IsNullOrEmpty(SettingsService.Instance.Settings.Username))
            {
                var vault = new PasswordVault();
                var credentials = vault.Retrieve(
                    SettingsService.Instance.Settings.ServerAddress,
                    SettingsService.Instance.Settings.Username
                );

                if (credentials != null)
                {
                    _client = new NextcloudClient.NextcloudClient(
                        credentials.Resource,
                        credentials.UserName,
                        credentials.Password
                        );
                }
            }

            SettingsService.Instance.Settings.PropertyChanged += (sender, args) =>
            {
                if (
                    string.IsNullOrEmpty(SettingsService.Instance.Settings.ServerAddress) ||
                    string.IsNullOrEmpty(SettingsService.Instance.Settings.Username)
                    )
                {
                    return;
                }

                var vault = new PasswordVault();
                var credentials = vault.Retrieve(
                    SettingsService.Instance.Settings.ServerAddress,
                    SettingsService.Instance.Settings.Username
                );

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
