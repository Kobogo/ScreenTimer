using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ScreenTimer
{
    public partial class InactiveWindow : Window
    {
        private DispatcherTimer _blinkTimer;
        private bool _isWhiteState = false;

        public InactiveWindow()
        {
            InitializeComponent();

            // Sæt timeren til at køre hvert 500. millisekund (0,5 sek)
            _blinkTimer = new DispatcherTimer();
            _blinkTimer.Interval = TimeSpan.FromMilliseconds(500);
            _blinkTimer.Tick += BlinkTimer_Tick;
            _blinkTimer.Start();
        }

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            if (_isWhiteState)
            {
                // Skift til Sort baggrund / Hvid tekst
                BlinkBorder.Background = System.Windows.Media.Brushes.Black;
                BlinkText1.Foreground = System.Windows.Media.Brushes.White;
            }
            else
            {
                // Skift til Hvid baggrund / Sort tekst
                BlinkBorder.Background = System.Windows.Media.Brushes.White;
                BlinkText1.Foreground = System.Windows.Media.Brushes.Black;
            }

            _isWhiteState = !_isWhiteState;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Stop timeren og luk vinduet
            _blinkTimer?.Stop();
            this.Close();
        }

        // Sikkerhed hvis vinduet lukkes på andre måder
        protected override void OnClosed(EventArgs e)
        {
            _blinkTimer?.Stop();
            base.OnClosed(e);
        }
    }
}