using System.Windows;
using MicrophoneListener.Views;
using Prism.Ioc;
using RestoreWindowPlace;

namespace MicrophoneListener
{
    public partial class App
    {
        private readonly WindowPlace windowPlace;

        public App()
        {
            windowPlace = new WindowPlace("window.config");
            Exit += OnExit;
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            windowPlace.Save();
        }

        protected override void RegisterTypes(IContainerRegistry o)
        {
        }

        protected override Window CreateShell()
        {
            var mainWindow = Container.Resolve<MainWindow>();
            windowPlace.Register(mainWindow);
            return mainWindow;
        }

    }
}