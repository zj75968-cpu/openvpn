using System;
using System.Windows;
using TopEdgeTodo.Services;
using TopEdgeTodo.ViewModels;

namespace TopEdgeTodo.Views
{
    public partial class MainWindow : Window
    {
        private TopEdgeDetector? _detector;
        private WindowRevealController? _revealController;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            Width = vm.Settings.WindowWidth;
            Height = vm.Settings.WindowHeight;

            _revealController = new WindowRevealController(this, vm.Settings);
            _revealController.Attach();

            _detector = new TopEdgeDetector(vm.Settings);
            _detector.TopEdgeReached += (_, _) => Dispatcher.Invoke(() => _revealController?.Reveal());
            _detector.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.Settings.WindowWidth = Width;
                vm.Settings.WindowHeight = Height;
                vm.Settings.Save();
            }
            _detector?.Dispose();
            base.OnClosed(e);
        }
    }
}
