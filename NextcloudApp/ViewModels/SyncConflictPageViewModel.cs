using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using NextcloudApp.Converter;
using NextcloudApp.Models;
using NextcloudApp.Services;
using NextcloudApp.Utils;
using NextcloudClient.Exceptions;
using Prism.Commands;
using Prism.Windows.Navigation;
using NextcloudClient.Types;
using Prism.Windows.AppModel;

namespace NextcloudApp.ViewModels
{
    public class SyncConflictPageViewModel : ViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IResourceLoader _resourceLoader;
        private readonly DialogService _dialogService;
        public ICommand FixConflictByLocalCommand { get; private set; }

        public SyncConflictPageViewModel(INavigationService navigationService, IResourceLoader resourceLoader, DialogService dialogService)
        {
            _navigationService = navigationService;
            _resourceLoader = resourceLoader;
            _dialogService = dialogService;
            FixConflictByLocalCommand = new DelegateCommand(FixConflictByLocal);
        }
        
        private async void FixConflictByLocal()
        {
        
        }
    }
}
