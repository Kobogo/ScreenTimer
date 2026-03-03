using System;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms; // Til Tray Icon
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32; // Til Autostart

namespace ScreenTimer
{
    public partial class MainWindow : Window
    {
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private bool _isMini = false;
        private NotifyIcon _notifyIcon = null!;
        private int _totalSecondsLeft = 0;
        private int _initialTotalSeconds = 0;
        private bool _isRunning = false;
        private DispatcherTimer? _localTimer;
        private readonly HttpClient _client = new HttpClient();

        private readonly MediaPlayer _soundPlayer = new MediaPlayer();
        private bool _hasWarnedYellow = false;
        private bool _hasWarnedRed = false;

        private int _userId = 3;
        private string _apiUrl = "https://todoapi-spz1.onrender.com/api/Timer";
        private string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public MainWindow()
        {
            InitializeComponent();

            // Tving manuel styring af position før indlæsning
            this.WindowStartupLocation = WindowStartupLocation.Manual;

            SetAutostart();
            LoadSettings();
            SetupTimer();
            FetchInitialData();
            SetupTrayIcon();

            _soundPlayer.MediaEnded += (s, e) =>
            {
                _soundPlayer.Position = TimeSpan.Zero;
                _soundPlayer.Play();
            };

            // Gem position når vinduet flyttes
            this.LocationChanged += (s, e) => SaveSettings();
        }

        private void SetAutostart()
        {
            try
            {
                RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    key.SetValue("ScreenTimer", "\"" + Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe") + "\"");
                }
            }
            catch { }
        }

        private void LoadSettings()
        {
            if (File.Exists(_settingsPath))
            {
                try
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonConvert.DeserializeObject<JObject>(json);

                    if (settings != null)
                    {
                        this.Left = settings.Value<double>("Left");
                        this.Top = settings.Value<double>("Top");
                        _isMini = settings.Value<bool>("IsMini");

                        if (_isMini) ApplyMiniLayout(); else ApplyNormalLayout();
                    }
                }
                catch
                {
                    this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }
            else
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Vi tjekker om vinduet er i "Normal" tilstand før vi gemmer,
                // så vi ikke gemmer koordinater for minimeret tilstand (-32000)
                if (this.WindowState == WindowState.Normal)
                {
                    var settings = new { Left = this.Left, Top = this.Top, IsMini = _isMini };
                    File.WriteAllText(_settingsPath, JsonConvert.SerializeObject(settings));
                }
            }
            catch { }
        }

       private void SetupTrayIcon()
        {
            try
            {
                _notifyIcon = new NotifyIcon();
                var assembly = Assembly.GetExecutingAssembly();

                // Vi prøver at finde ressourcen (tjek at navnet matcher din fil i Solution Explorer)
                string resourceName = "ScreenTimer.timerOrange.ico";
                var stream = assembly.GetManifestResourceStream(resourceName)
                            ?? assembly.GetManifestResourceStream("timerOrange.ico");

                if (stream != null)
                {
                    // Vi indlæser ikonet direkte fra streamen uden at ændre farverne
                    var icon = new System.Drawing.Icon(stream);
                    _notifyIcon.Icon = icon;

                    // Sæt også vinduets ikon (oppe i venstre hjørne og på proceslinjen)
                    // Vi bruger BitmapFrame for at bevare kvaliteten
                    stream.Position = 0; // Reset stream position så den kan læses igen
                    this.Icon = BitmapFrame.Create(stream);
                }
                else
                {
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }

                _notifyIcon.Visible = true;
                _notifyIcon.Text = "ScreenTimer";
                _notifyIcon.DoubleClick += (s, e) =>
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate(); // Bringer vinduet forrest
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Fejl ved Tray Icon: " + ex.Message);
                if (_notifyIcon != null) _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
        }

        private void SetupTimer()
        {
            _localTimer = new DispatcherTimer();
            _localTimer.Interval = TimeSpan.FromSeconds(1);
            _localTimer.Tick += LocalTimer_Tick;
        }

        private async void FetchInitialData()
        {
            try
            {
                lblTime.Text = "WAKING...";
                await _client.PostAsync($"{_apiUrl}/reset-daily-time/{_userId}", null);
                var response = await _client.GetStringAsync($"{_apiUrl}/{_userId}");
                var data = JsonConvert.DeserializeObject<JObject>(response);

                if (data != null)
                {
                    int mins = data.GetValue("minutesLeftToday", StringComparison.OrdinalIgnoreCase)?.Value<int>() ?? 0;
                    _isRunning = data.GetValue("isTimerRunning", StringComparison.OrdinalIgnoreCase)?.Value<bool>() ?? false;
                    _totalSecondsLeft = mins * 60;
                    _initialTotalSeconds = _totalSecondsLeft;
                    _hasWarnedYellow = false; _hasWarnedRed = false;
                    UpdateDisplay();
                    if (_isRunning && _totalSecondsLeft > 0) _localTimer.Start(); else _localTimer.Stop();
                }
            }
            catch { lblTime.Text = "OFFLINE"; MainBorder.BorderBrush = System.Windows.Media.Brushes.Red; }
        }

        private void LocalTimer_Tick(object sender, EventArgs e)
        {
            if (_totalSecondsLeft > 0)
            {
                _totalSecondsLeft--;
                if (_totalSecondsLeft > 0 && _totalSecondsLeft % 60 == 0) SyncWithApi(1);
                if (_totalSecondsLeft == 0)
                {
                    SyncWithApi(1); _isRunning = false; _localTimer.Stop();
                    UpdateServerRunningStatus(false);
                }
            }
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            TimeSpan t = TimeSpan.FromSeconds(_totalSecondsLeft);
            lblTime.Text = (_isMini && _totalSecondsLeft < 3600) ? t.ToString(@"mm\:ss") : t.ToString(@"hh\:mm\:ss");

            if (MainBorder != null)
            {
                if (_totalSecondsLeft <= 0)
                {
                    MainBorder.BorderBrush = System.Windows.Media.Brushes.Red;
                    if (!_hasWarnedRed) { _hasWarnedRed = true; TriggerWarning("quack-sound-effect.mp3"); }
                }
                else if (_totalSecondsLeft <= (_initialTotalSeconds / 2))
                {
                    MainBorder.BorderBrush = System.Windows.Media.Brushes.Yellow;
                    if (!_hasWarnedYellow) { _hasWarnedYellow = true; TriggerWarning("duck-toy-sound.mp3"); }
                }
                else
                {
                    MainBorder.BorderBrush = System.Windows.Media.Brushes.LimeGreen;
                    if (_totalSecondsLeft > (_initialTotalSeconds / 2) + 10) _hasWarnedYellow = false;
                    if (_totalSecondsLeft > 10) _hasWarnedRed = false;
                }
            }
            btnToggle.Content = _isMini ? (_isRunning ? "\uE103" : "\uE102") : (_isRunning ? "PAUSE" : "START");
            btnSync.Content = _isMini ? "\uE149" : "SYNC";
        }

        private void TriggerWarning(string soundFileName)
        {
            string soundPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, soundFileName);
            if (File.Exists(soundPath))
            {
                _soundPlayer.Open(new Uri(soundPath));
                _soundPlayer.Play();
                var stopTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
                stopTimer.Tick += (s, args) => { _soundPlayer.Stop(); ((DispatcherTimer)s).Stop(); };
                stopTimer.Start();
            }

            if (MainBorder != null)
            {
                Storyboard sb = new Storyboard();
                ThicknessAnimation borderAnim = new ThicknessAnimation
                {
                    From = new Thickness(2),
                    To = new Thickness(10),
                    Duration = TimeSpan.FromSeconds(0.4),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(10)
                };
                Storyboard.SetTarget(borderAnim, MainBorder);
                Storyboard.SetTargetProperty(borderAnim, new PropertyPath(Border.BorderThicknessProperty));

                DoubleAnimation opacityAnim = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.4,
                    Duration = TimeSpan.FromSeconds(0.4),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(10)
                };
                Storyboard.SetTarget(opacityAnim, this);
                Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(Window.OpacityProperty));

                sb.Children.Add(borderAnim);
                sb.Children.Add(opacityAnim);
                sb.Begin();
            }
        }

        private void btnSizeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isMini = !_isMini;
            if (_isMini) ApplyMiniLayout(); else ApplyNormalLayout();
            SaveSettings();
            UpdateDisplay();
        }

        private void ApplyMiniLayout()
        {
            this.Width = 180; this.Height = 45;
            MainContainer.Orientation = System.Windows.Controls.Orientation.Horizontal;
            lblTime.FontSize = 22;
            MainBorder.CornerRadius = new CornerRadius(8);
            btnSizeToggle.Content = "\uE745";
            btnToggle.Width = 30; btnToggle.Height = 30; btnToggle.Margin = new Thickness(0);
            btnToggle.Background = System.Windows.Media.Brushes.Transparent;
            btnToggle.FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets");
            btnToggle.FontSize = 14;
            btnSync.Width = 30; btnSync.Height = 30; btnSync.Margin = new Thickness(0);
            btnSync.Background = System.Windows.Media.Brushes.Transparent;
            btnSync.FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets");
            btnSync.FontSize = 14;
            ButtonPanel.Margin = new Thickness(0);
            MainContainer.Margin = new Thickness(10, 0, 0, 0);
        }

        private void ApplyNormalLayout()
        {
            this.Width = 250; this.Height = 120;
            MainContainer.Orientation = System.Windows.Controls.Orientation.Vertical;
            MainContainer.Margin = new Thickness(0);
            lblTime.FontSize = 36;
            MainBorder.CornerRadius = new CornerRadius(15);
            btnSizeToggle.Content = "\uE744";
            btnToggle.Width = 65; btnToggle.Height = 25; btnToggle.Margin = new Thickness(5);
            btnToggle.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(68, 68, 68));
            btnToggle.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            btnToggle.FontSize = 12;
            btnSync.Width = 65; btnSync.Height = 25; btnSync.Margin = new Thickness(5);
            btnSync.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(68, 68, 68));
            btnSync.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            btnSync.FontSize = 12;
            ButtonPanel.Margin = new Thickness(5);
        }

        public async void btnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_totalSecondsLeft <= 0 && !_isRunning) return;
            _isRunning = !_isRunning;
            UpdateServerRunningStatus(_isRunning);
            if (_isRunning) _localTimer.Start(); else _localTimer.Stop();
            UpdateDisplay();
        }

        private async void UpdateServerRunningStatus(bool running)
        {
            try {
                var json = JsonConvert.SerializeObject(running);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PatchAsync($"{_apiUrl}/{_userId}/toggle", content);
            } catch { }
        }

        private async void SyncWithApi(int minutesUsed)
        {
            try {
                var json = JsonConvert.SerializeObject(minutesUsed);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PatchAsync($"{_apiUrl}/{_userId}/sync", content);
            } catch { }
        }

        public void btnSync_Click(object sender, RoutedEventArgs e) => FetchInitialData();

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left) DragMove();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized) this.Hide();
            base.OnStateChanged(e);
        }
    }
}