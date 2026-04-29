using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ScreenTimer.ViewModels;
using ScreenTimer.Services;

namespace ScreenTimer
{
    public partial class MainWindow : Window
    {
        private readonly SettingsService _settingsService;
        private InactiveWindow _currentPopup;

        public MainWindow()
        {
            InitializeComponent();
            _settingsService = new SettingsService();

            var vm = new MainViewModel();
            this.DataContext = vm;

            // 1. Hent og påfør gemte indstillinger med skærm-tjek
            var (left, top, isMini) = _settingsService.LoadSettings();

            if (!double.IsNaN(left) && !double.IsNaN(top))
            {
                // Sikkerheds-tjek: Er koordinaterne synlige på de nuværende skærme?
                // Vi tjekker VirtualScreen for at tage højde for alle tilsluttede skærme.
                double virtualLeft = SystemParameters.VirtualScreenLeft;
                double virtualTop = SystemParameters.VirtualScreenTop;
                double virtualWidth = SystemParameters.VirtualScreenWidth;
                double virtualHeight = SystemParameters.VirtualScreenHeight;

                // Hvis vinduet er uden for det synlige område, centrerer vi det i stedet
                if (left < virtualLeft || left > (virtualLeft + virtualWidth - 50) ||
                    top < virtualTop || top > (virtualTop + virtualHeight - 50))
                {
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
                else
                {
                    this.WindowStartupLocation = WindowStartupLocation.Manual;
                    this.Left = left;
                    this.Top = top;
                }
            }
            else
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            vm.IsMini = isMini;

            // 2. Lyt efter ændringer i ViewModel
            vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.IsMini))
                {
                    Dispatcher.Invoke(() => {
                        if (vm.IsMini) ApplyMiniLayout(); else ApplyNormalLayout();
                    });
                }

                if (e.PropertyName == nameof(MainViewModel.ShowInactivePopup))
                {
                    Dispatcher.Invoke(() => {
                        HandleInactivePopup(vm);
                    });
                }
            };

            this.Loaded += (s, e) => {
                if (vm.IsMini) ApplyMiniLayout(); else ApplyNormalLayout();
            };
        }

        private void HandleInactivePopup(MainViewModel vm)
        {
            if (vm.ShowInactivePopup)
            {
                if (_currentPopup == null)
                {
                    _currentPopup = new InactiveWindow();
                    _currentPopup.DataContext = vm;

                    _currentPopup.Left = this.Left + (this.Width / 2) - (_currentPopup.Width / 2);
                    _currentPopup.Top = this.Top - _currentPopup.Height - 10;

                    _currentPopup.Closed += (s, args) => _currentPopup = null;
                    _currentPopup.Show();
                }
            }
            else
            {
                _currentPopup?.Close();
                _currentPopup = null;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (this.DataContext is MainViewModel vm)
            {
                // Vi gemmer de aktuelle koordinater
                _settingsService.SaveSettings(this.Left, this.Top, vm.IsMini);
            }

            _currentPopup?.Close();
            base.OnClosing(e);
        }

        private void ApplyMiniLayout()
        {
            this.Width = 255;
            this.Height = 45;
            MainContainer.Orientation = System.Windows.Controls.Orientation.Horizontal;
            MainContainer.Margin = new Thickness(0);
            MainBorder.CornerRadius = new CornerRadius(10);
            ButtonPanel.Margin = new Thickness(0);

            btnToggle.Width = 35;
            btnToggle.Height = 30;
            btnToggle.Background = System.Windows.Media.Brushes.Transparent;
            btnToggle.FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets");
            btnToggle.FontSize = 18;

            btnSync.Width = 35;
            btnSync.Height = 30;
            btnSync.Content = "\uE117";
            btnSync.Background = System.Windows.Media.Brushes.Transparent;
            btnSync.FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets");
            btnSync.FontSize = 18;
        }

        private void ApplyNormalLayout()
        {
            this.Width = 250;
            this.Height = 120;
            MainContainer.Orientation = System.Windows.Controls.Orientation.Vertical;
            MainContainer.Margin = new Thickness(0);
            MainBorder.CornerRadius = new CornerRadius(15);

            btnToggle.Width = 65;
            btnToggle.Height = 25;
            btnToggle.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#444"));
            btnToggle.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            btnToggle.FontSize = 12;

            btnSync.Width = 65;
            btnSync.Height = 25;
            btnSync.Content = "SYNC";
            btnSync.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#444"));
            btnSync.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            btnSync.FontSize = 12;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }
    }
}