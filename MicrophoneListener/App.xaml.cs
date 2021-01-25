using System.Windows;
using MicrophoneListener.Views;
using Prism.Ioc;

namespace MicrophoneListener
{
    public partial class App
    {
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }
    }
}