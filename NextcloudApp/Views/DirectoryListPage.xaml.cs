using NextcloudApp.ViewModels;

namespace NextcloudApp.Views
{
    public sealed partial class DirectoryListPage
    {
        public DirectoryListPage()
        {
            InitializeComponent();
            var dataContext = DataContext as DirectoryListPageViewModel;
            dataContext.ListView = FileOrFolderListView;
        }
    }
}
