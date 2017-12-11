using Microsoft.Xaml.Interactivity;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace NextcloudApp.Behaviors
{
    internal class ListViewConnectedAnimationBehavior : DependencyObject, IBehavior
    {
        private FrameworkElement _element;
        private ListView _listView;
        private ScrollViewer _scrollViewer;
        public DependencyObject AssociatedObject => _element;

        public void Attach(DependencyObject associatedObject)
        {
            _element = (FrameworkElement)associatedObject;
            _listView = (ListView)AssociatedObject;
            _listView.SelectionChanged += SelectionChanged;
        }
        
        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_listView.SelectionMode == ListViewSelectionMode.Single)
            {
                var el = (FrameworkElement)_listView.ContainerFromIndex(0);
                if (el == null)
                {
                    return;
                }
                var img = FindElementByName<Image>(el, "Thumbnail");
                if (img == null)
                {
                    return;
                }
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("image", img);
            }

        }

        public void Detach()
        {
            _listView.SelectionChanged -= SelectionChanged;
        }
        
        /// <summary>
        /// Extension method for a FrameworkElement that searches for a child element by type and name.
        /// </summary>
        /// <typeparam name="T">The type of the child element to search for.</typeparam>
        /// <param name="element">The parent framework element.</param>
        /// <param name="sChildName">The name of the child element to search for.</param>
        /// <returns>The matching child element, or null if none found.</returns>
        public static T FindElementByName<T>(FrameworkElement element, string sChildName) where T : FrameworkElement
        {
            Debug.WriteLine("[FindElementByName] ==> element [{0}] sChildName [{1}] T [{2}]", element, sChildName, typeof(T).ToString());

            T childElement = null;

            //
            // Spin through immediate children of the starting element.
            //
            var nChildCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < nChildCount; i++)
            {
                // Get next child element.
                FrameworkElement child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                Debug.WriteLine("Found child [{0}]", child);

                // Do we have a child?
                if (child == null)
                    continue;

                // Is child of desired type and name?
                if (child is T && child.Name.Equals(sChildName))
                {
                    // Bingo! We found a match.
                    childElement = (T)child;
                    Debug.WriteLine("Found matching element [{0}]", childElement);
                    break;
                } // if

                // Recurse and search through this child's descendants.
                childElement = FindElementByName<T>(child, sChildName);

                // Did we find a matching child?
                if (childElement != null)
                    break;
            } // for

            Debug.WriteLine("[FindElementByName] <== childElement [{0}]", childElement);
            return childElement;
        }
    }
}
