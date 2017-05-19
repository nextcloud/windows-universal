using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.Xaml.Interactivity;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls;

namespace NextcloudApp.Actions
{
    public class OpenMenuFlyoutAction : DependencyObject, IAction
    {
        public object Execute(object sender, object parameter)
        {
            var senderElement = sender as FrameworkElement;
            var args = parameter as RightTappedRoutedEventArgs;
            var flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            var menuFlyout = flyoutBase as MenuFlyout;
            menuFlyout.ShowAt(senderElement, args.GetPosition(senderElement));

            return null;
        }
    }
}
