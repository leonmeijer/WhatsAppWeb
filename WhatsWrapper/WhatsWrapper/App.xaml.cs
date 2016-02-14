using System;
using System.Threading.Tasks;
using Template10.Services.SettingsService;
using WhatsWrapper.ViewModels;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Resources;

namespace WhatsWrapper
{
    /// Documentation on APIs used in this page:
    /// https://github.com/Windows-XAML/Template10/wiki

    sealed partial class App : Template10.Common.BootStrapper
    {
        public static ResourceLoader loader;

        public App()
        {
            InitializeComponent();
            loader = new ResourceLoader();
        }

        public override Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            NavigationService.Navigate(typeof(Views.MainPage));

            return Task.CompletedTask;
        }
    }
}

