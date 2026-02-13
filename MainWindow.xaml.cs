using System;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;

namespace ScreenTimer
{
    public partial class MainWindow : Window
    {
        private NotifyIcon _notifyIcon;
        private int _totalSecondsLeft = 0;
        private bool _isRunning = false;
        private DispatcherTimer? _localTimer;
        private readonly HttpClient _client = new HttpClient();

        // RET DISSE TO:
        private int _userId = 3;
        private string _apiUrl = "https://todoapi-spz1.onrender.com/api/Timer";

        public MainWindow()
        {
            InitializeComponent();
            SetupTimer();
            FetchInitialData();
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = new System.Drawing.Icon("timer.ico");
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "ScreenTimer";
            _notifyIcon.DoubleClick += (s, e) => {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
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

                // Robust parsing der håndterer både "minutesLeftToday" og "MinutesLeftToday"
                var data = JsonConvert.DeserializeObject<JObject>(response);

                if (data != null)
                {
                    int mins = data.GetValue("minutesLeftToday", StringComparison.OrdinalIgnoreCase)?.Value<int>() ?? 0;
                    _isRunning = data.GetValue("isTimerRunning", StringComparison.OrdinalIgnoreCase)?.Value<bool>() ?? false;

                    _totalSecondsLeft = mins * 60;

                    UpdateDisplay();

                    if (_isRunning && _totalSecondsLeft > 0)
                        _localTimer.Start();
                    else
                        _localTimer.Stop();

                    btnToggle.Content = _isRunning ? "PAUSE" : "START";
                }
            }
            catch
            {
                lblTime.Text = "OFFLINE";
                MainBorder.BorderBrush = System.Windows.Media.Brushes.Red;
            }
        }

        private void LocalTimer_Tick(object sender, EventArgs e)
        {
            if (_totalSecondsLeft > 0)
            {
                _totalSecondsLeft--;

                // 1. Almindelig sync hvert minut (f.eks. ved 180, 120, 60 sekunder tilbage)
                if (_totalSecondsLeft > 0 && _totalSecondsLeft % 60 == 0)
                {
                    SyncWithApi(1);
                }

                // 2. Den "mangler" sync: Når vi rammer præcis 0, sender vi det sidste minut ind
                if (_totalSecondsLeft == 0)
                {
                    SyncWithApi(1);
                    _isRunning = false;
                    _localTimer.Stop();
                    btnToggle.Content = "START";

                    // Valgfrit: Sæt IsTimerRunning til false i databasen også
                    UpdateServerRunningStatus(false);
                }
            }
            UpdateDisplay();
        }

        private async void UpdateServerRunningStatus(bool running)
        {
            try
            {
                var json = JsonConvert.SerializeObject(running);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PatchAsync($"{_apiUrl}/{_userId}/toggle", content);
            }
            catch { }
        }

        private async void SyncWithApi(int minutesUsed)
        {
            try
            {
                // Vi sender blot tallet 1 som body, da dit API forventer [FromBody] int minutesUsed
                var json = JsonConvert.SerializeObject(minutesUsed);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PatchAsync($"{_apiUrl}/{_userId}/sync", content);
            }
            catch
            {
                // Fejl ved sync ignoreres for ikke at forstyrre brugeren
            }
        }

        private void UpdateDisplay()
        {
            // Omdan de totale sekunder til et pænt format (HH:mm:ss)
            TimeSpan t = TimeSpan.FromSeconds(_totalSecondsLeft);
            lblTime.Text = t.ToString(@"hh\:mm\:ss");

            if (MainBorder != null)
            {
                if (_totalSecondsLeft <= 0)
                {
                    // 0 minutter tilbage = RØD
                    MainBorder.BorderBrush = System.Windows.Media.Brushes.Red;
                }
                else if (_totalSecondsLeft <= 30 * 60)
                {
                    // 30 minutter (30 * 60 sekunder) eller mindre = GUL
                    MainBorder.BorderBrush = System.Windows.Media.Brushes.Yellow;
                }
                else
                {
                    // Over 30 minutter = GRØN
                    MainBorder.BorderBrush = System.Windows.Media.Brushes.LimeGreen;
                }
            }
        }

        public async void btnToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_totalSecondsLeft <= 0 && !_isRunning) return; // Start ikke hvis der ikke er tid

            _isRunning = !_isRunning;

            try
            {
                var json = JsonConvert.SerializeObject(_isRunning);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PatchAsync($"{_apiUrl}/{_userId}/toggle", content);
            }
            catch { }

            if (_isRunning) _localTimer.Start(); else _localTimer.Stop();
            btnToggle.Content = _isRunning ? "PAUSE" : "START";
        }

        public void btnSync_Click(object sender, RoutedEventArgs e) => FetchInitialData();

        public void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
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