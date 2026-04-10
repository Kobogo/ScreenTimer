using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using ScreenTimer.Services;

namespace ScreenTimer.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ApiService _apiService;
        private readonly SettingsService _settingsService;
        private readonly NotificationService _notificationService;
        private readonly DispatcherTimer _timer;

        private int _totalSecondsLeft;
        private int _initialTotalSeconds;
        private bool _hasWarnedYellow;
        private bool _hasWarnedRed;
        private string _timerDisplay = "00:00:00";
        private bool _showInactivePopup;

        #region Windows API
        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        private static uint GetIdleTime()
        {
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            if (!GetLastInputInfo(ref lastInputInfo)) return 0;
            return ((uint)Environment.TickCount - lastInputInfo.dwTime) / 1000;
        }
        #endregion

        public bool ShowInactivePopup
        {
            get => _showInactivePopup;
            set
            {
                _showInactivePopup = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(InactivePopupVisibility));
                OnPropertyChanged(nameof(MainContentVisibility));
            }
        }

        public Visibility InactivePopupVisibility => ShowInactivePopup ? Visibility.Visible : Visibility.Collapsed;
        public Visibility MainContentVisibility => ShowInactivePopup ? Visibility.Collapsed : Visibility.Visible;

        public string TimerDisplay
        {
            get => _timerDisplay;
            set { _timerDisplay = value; OnPropertyChanged(); }
        }

        private string _saturdayBonusPot = "0";
        public string SaturdayBonusPot
        {
            get => _saturdayBonusPot;
            set
            {
                _saturdayBonusPot = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedBonusPot));
            }
        }

        public string FormattedBonusPot
        {
            get
            {
                if (int.TryParse(SaturdayBonusPot, out int totalMins))
                {
                    int hrs = totalMins / 60;
                    int mins = totalMins % 60;
                    return string.Format("{0:00}:{1:00}:00", hrs, mins);
                }
                return "00:00:00";
            }
        }

        public Thickness TimerMargin => (IsMini && ShowBonusInMini) ? new Thickness(0) : new Thickness(0, 0, 20, 0);

        private bool _showBonusInMini;
        public bool ShowBonusInMini
        {
            get => _showBonusInMini;
            set
            {
                _showBonusInMini = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimerMargin));
                UpdateDisplay();
            }
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ToggleButtonText));
                OnPropertyChanged(nameof(ToggleIcon));
            }
        }

        private bool _isMini;
        public bool IsMini
        {
            get => _isMini;
            set
            {
                _isMini = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SizeIcon));
                OnPropertyChanged(nameof(TimerFontSize));
                OnPropertyChanged(nameof(ToggleButtonText));
                OnPropertyChanged(nameof(NormalBonusVisibility));
                OnPropertyChanged(nameof(TimerMargin));
                if (!_isMini) ShowBonusInMini = false;
                UpdateDisplay();
            }
        }

        public double TimerFontSize => IsMini ? 24 : 36;
        public Visibility NormalBonusVisibility => IsMini ? Visibility.Collapsed : Visibility.Visible;
        public bool IsTimerControlsVisible => !(IsMini && ShowBonusInMini);

        public string ToggleButtonText => IsMini ? (IsRunning ? "\uE103" : "\uE102") : (IsRunning ? "PAUSE" : "START");
        public string SizeIcon => IsMini ? "\uE745" : "\uE744";
        public string ToggleIcon => IsRunning ? "\uE103" : "\uE102";

        public ICommand ToggleCommand { get; }
        public ICommand SyncCommand { get; }
        public ICommand ToggleSizeCommand { get; }
        public ICommand ToggleBonusCommand { get; }
        public ICommand ResumeFromInactivityCommand { get; }

        public MainViewModel()
        {
            _apiService = new ApiService();
            _settingsService = new SettingsService();
            _notificationService = new NotificationService();

            ToggleCommand = new RelayCommand(_ => ToggleTimer());
            SyncCommand = new RelayCommand(_ => InitializeAsync());
            ToggleSizeCommand = new RelayCommand(_ => ToggleSize());
            ToggleBonusCommand = new RelayCommand(_ => ShowBonusInMini = !ShowBonusInMini);
            ResumeFromInactivityCommand = new RelayCommand(_ => {
                ShowInactivePopup = false;
                if (!IsRunning) ToggleTimer();
            });

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;

            var settings = _settingsService.LoadSettings();
            IsMini = settings.IsMini;

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                TimerDisplay = "WAKING...";
                var data = await _apiService.FetchTimerDataAsync();
                if (data != null)
                {
                    int mins = data.Value<int>("minutesLeftToday");
                    _totalSecondsLeft = mins * 60;
                    _initialTotalSeconds = _totalSecondsLeft;
                    SaturdayBonusPot = data["saturdayBonusPot"]?.ToString() ?? "0";
                    IsRunning = data.Value<bool>("isTimerRunning");
                    _hasWarnedYellow = _hasWarnedRed = false;
                    UpdateDisplay();
                    if (IsRunning && _totalSecondsLeft > 0) _timer.Start();
                }
                else
                {
                    TimerDisplay = "OFFLINE";
                    BorderColor = System.Windows.Media.Brushes.Red;
                }
            }
            catch
            {
                TimerDisplay = "ERROR";
                BorderColor = System.Windows.Media.Brushes.Orange;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (IsRunning && _totalSecondsLeft > 0)
            {
                if (GetIdleTime() >= 900) // 15 minutter inaktivitet
                {
                    HandleInactivity();
                    return;
                }

                _totalSecondsLeft--;
                if (_totalSecondsLeft % 60 == 0) _apiService.SyncMinutesAsync(1);
                UpdateDisplay();
                CheckWarnings();
            }
            else if (_totalSecondsLeft <= 0)
            {
                _timer.Stop();
                IsRunning = false;
                _apiService.UpdateStatusAsync(false);
                UpdateDisplay();
            }
        }

        private void HandleInactivity()
        {
            _timer.Stop();
            IsRunning = false;
            _apiService.UpdateStatusAsync(false);

            // Sikr at UI opdateres på hovedtråden
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                ShowInactivePopup = true;
            });
        }

        private void ToggleTimer()
        {
            if (_totalSecondsLeft <= 0 && !IsRunning) return;
            IsRunning = !IsRunning;
            if (IsRunning) _timer.Start(); else _timer.Stop();
            _apiService.UpdateStatusAsync(IsRunning);
        }

        private void ToggleSize() => IsMini = !IsMini;

        private void UpdateDisplay()
        {
            if (IsMini && ShowBonusInMini)
            {
                TimerDisplay = FormattedBonusPot;
            }
            else
            {
                TimeSpan t = TimeSpan.FromSeconds(_totalSecondsLeft);
                TimerDisplay = string.Format("{0:00}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds);
            }
            OnPropertyChanged(nameof(IsTimerControlsVisible));
            OnPropertyChanged(nameof(ToggleButtonText));
        }

        private System.Windows.Media.Brush _borderColor = System.Windows.Media.Brushes.LimeGreen;
        public System.Windows.Media.Brush BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; OnPropertyChanged(); }
        }

        private void CheckWarnings()
        {
            if (_totalSecondsLeft <= 0)
            {
                BorderColor = System.Windows.Media.Brushes.Red;
                if (!_hasWarnedRed) { _hasWarnedRed = true; _notificationService.PlayWarningSound("quack-sound-effect.mp3"); }
            }
            else if (_totalSecondsLeft <= (_initialTotalSeconds / 2))
            {
                BorderColor = System.Windows.Media.Brushes.Yellow;
                if (!_hasWarnedYellow) { _hasWarnedYellow = true; _notificationService.PlayWarningSound("duck-toy-sound.mp3"); }
            }
            else { BorderColor = System.Windows.Media.Brushes.LimeGreen; }
        }
    }
}