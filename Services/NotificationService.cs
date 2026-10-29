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

        public void PlayWarningSound(string filePathOrName)
        {
            string path = filePathOrName;

            // Hvis det bare er et filnavn (f.eks. standardlyden) uden stiniveau, kombinerer vi med basismappen
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePathOrName);
            }

            // Hvis filen ikke findes på den angivne sti, falder vi tilbage til standardlyden
            if (!File.Exists(path))
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "duck-toy-sound.mp3");
            }

            if (File.Exists(path))
            {
                try
                {
                    _soundPlayer.Open(new Uri(path));
                    _soundPlayer.Play();

                    var stopTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                    stopTimer.Tick += (s, args) => { _soundPlayer.Stop(); ((DispatcherTimer)s).Stop(); };
                    stopTimer.Start();
                }
                catch
                {
                    // Håndter evt. uventede mediefejl
                }
            }
        }

        public void StopSound() => _soundPlayer.Stop();
    }
}