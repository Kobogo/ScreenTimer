using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Forms;

namespace ScreenTimer.Services
{
    public class NotificationService
    {
        private readonly MediaPlayer _soundPlayer = new MediaPlayer();

        public NotificationService()
        {
            _soundPlayer.MediaEnded += (s, e) =>
            {
                _soundPlayer.Position = TimeSpan.Zero;
                _soundPlayer.Play();
            };
        }

        public void PlayWarningSound(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (File.Exists(path))
            {
                _soundPlayer.Open(new Uri(path));
                _soundPlayer.Play();

                var stopTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                stopTimer.Tick += (s, args) => { _soundPlayer.Stop(); ((DispatcherTimer)s).Stop(); };
                stopTimer.Start();
            }
        }

        public void StopSound() => _soundPlayer.Stop();
    }
}