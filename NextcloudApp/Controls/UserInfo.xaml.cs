using System;
using Windows.UI.Xaml.Controls;
using Windows.Networking.Connectivity;
using NextcloudApp.Converter;
using NextcloudApp.Services;
using NextcloudApp.Utils;
using NextcloudClient.Exceptions;
using NextcloudClient.Types;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NextcloudApp.Annotations;
using System.Globalization;

namespace NextcloudApp.Controls
{
    public partial class UserInfo : UserControl, INotifyPropertyChanged
    {
        private User _user;
        private Uri _userAvatarUrl;
        private string _quotaUsedOfTotalString;

        public UserInfo()
        {
            InitializeComponent();

            SettingsService.Default.Value.LocalSettings.PropertyChanged += (sender, args) =>
            {
                GetUserInformation();
            };
            GetUserInformation();
        }

        private async void GetUserInformation()
        {
            var client = await ClientService.GetClient();
            if (client == null)
            {
                return;
            }

            var username = SettingsService.Default.Value.LocalSettings.Username;

            if (string.IsNullOrEmpty(username))
                return;

            try
            {
                User = await client.GetUserAttributes(username);

                var converter = new BytesToHumanReadableConverter();
                QuotaUsedOfTotalString = LocalizationService.Instance.GetString(
                    "QuotaUsedOfTotal",
                    converter.Convert(User.Quota.Used, typeof(string), null, CultureInfo.CurrentCulture.ToString()),
                    converter.Convert(User.Quota.Total, typeof(string), null, CultureInfo.CurrentCulture.ToString())
                );

                switch (SettingsService.Default.Value.LocalSettings.PreviewImageDownloadMode)
                {
                    case PreviewImageDownloadMode.Always:
                        UserAvatarUrl = await client.GetUserAvatarUrl(username, 120);
                        break;
                    case PreviewImageDownloadMode.WiFiOnly:
                        var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                        // connectionProfile can be null (e.g. airplane mode)
                        if (connectionProfile != null && connectionProfile.IsWlanConnectionProfile)
                        {
                            UserAvatarUrl = await client.GetUserAvatarUrl(username, 120);
                        }
                        break;
                    case PreviewImageDownloadMode.Never:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (ResponseError e)
            {
                ResponseErrorHandlerService.HandleException(e);
            }
        }

        public string QuotaUsedOfTotalString
        {
            get => _quotaUsedOfTotalString;
            set {
                if (_quotaUsedOfTotalString == value)
                {
                    return;
                }
                _quotaUsedOfTotalString = value;
                OnPropertyChanged();
            }
        }

        public User User
        {
            get => _user;
            set {
                if (_user == value)
                {
                    return;
                }
                _user = value;
                OnPropertyChanged();
            }
        }

        public Uri UserAvatarUrl
        {
            get => _userAvatarUrl;
            set {
                if (_userAvatarUrl == value)
                {
                    return;
                }
                _userAvatarUrl = value;
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
