using System.Windows.Input;
using NextcloudApp.ViewModels;

namespace NextcloudApp.Models
{
    public class MenuItem : ViewModel
    {
        public string DisplayName { get; set; }

        public string FontIcon { get; set; }

        public ICommand Command { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
