using System;
using System.Windows.Input;
using JetBrains.Annotations;
using MicrophoneListener.ViewModels;

namespace MicrophoneListener.Views
{
    [UsedImplicitly]
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            Closed += OnClosed;
        }


        private void OnClosed(object sender, EventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.Dispose();
            }
        }

        private void MainWindow_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch
            {
                // ignored
            }
        }
    }
}