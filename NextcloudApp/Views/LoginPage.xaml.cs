using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Prism.Windows.Mvvm;

namespace NextcloudApp.Views
{
    public sealed partial class LoginPage : SessionStateAwarePage
    {
        public LoginPage()
        {
            InitializeComponent();
        }
        private void ConnectButton_OnClick(object sender, RoutedEventArgs e)
        {
        }
    }
}