using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NextcloudApp
{
    public sealed partial class AppShell : Page
    {
        public AppShell()
        {
            InitializeComponent();
        }

        public void SetContentFrame(Frame frame)
        {
            RootSplitView.Content = frame;
        }

        public void SetMenuPaneContent(UIElement content)
        {
            RootSplitView.Pane = content;
        }

        public UIElement GetContentFrame()
        {
            return RootSplitView.Content;
        }
    }
}
