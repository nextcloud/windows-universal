using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;

namespace NextcloudApp.Behaviors
{
    internal class ScrollListBoxToEndBehavior : DependencyObject, IBehavior
    {
        private FrameworkElement _element;
        private ListBox _listBox;
        private ObservableCollection<Models.PathInfo> _observable;
        public DependencyObject AssociatedObject => _element;

        public void Attach(DependencyObject associatedObject)
        {
            _element = (FrameworkElement)associatedObject;
            _listBox = (ListBox)AssociatedObject;
            _listBox.DataContextChanged += (sender, args) =>
            {
                AttachToItemSource();
            };
            _listBox.Loaded += (sender, args) =>
            {
                AttachToItemSource();
            };
        }

        private void AttachToItemSource()
        {
            _observable = _listBox.ItemsSource as ObservableCollection<Models.PathInfo>;
            if (_observable != null)
            {
                _observable.CollectionChanged += ObservableOnCollectionChanged;
            }
            var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(_listBox, 0);
            scrollViewer?.ChangeView(scrollViewer.ExtentWidth, 0, 1);
        }

        private void ObservableOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            // always scroll path to end
            if (VisualTreeHelper.GetChildrenCount(_listBox) <= 0)
            {
                return;
            }
            var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(_listBox, 0);
            scrollViewer.ChangeView(scrollViewer.ExtentWidth, 0, 1);
        }

        public void Detach()
        {
            if (_observable != null)
            {
                _observable.CollectionChanged -= ObservableOnCollectionChanged;
            }
        }
    }
}
