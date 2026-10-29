using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScreenTimer.Services
{
    public class SettingsService
    {
        private readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public (double Left, double Top, bool IsMini, string HalfTimeSoundPath, string EndTimeSoundPath) LoadSettings()
        {
            if (File.Exists(_path))
            {
                try
                {
                    var json = File.ReadAllText(_path);
                    var s = JsonConvert.DeserializeObject<JObject>(json);
                    if (s != null)
                    {
                        double left = s["Left"]?.Value<double>() ?? double.NaN;
                        double top = s["Top"]?.Value<double>() ?? double.NaN;
                        bool isMini = s["IsMini"]?.Value<bool>() ?? false;
                        string halfTimeSoundPath = s["HalfTimeSoundPath"]?.Value<string>() ?? "duck-toy-sound.mp3";
                        string endTimeSoundPath = s["EndTimeSoundPath"]?.Value<string>() ?? "quack-sound-effect.mp3";

                        return (left, top, isMini, halfTimeSoundPath, endTimeSoundPath);
                    }
                }
                catch
                {
                    // Standard ved læsefejl
                }
            }
            return (double.NaN, double.NaN, false, "duck-toy-sound.mp3", "quack-sound-effect.mp3");
        }

        public void SaveSettings(double left, double top, bool isMini, string halfTimeSoundPath, string endTimeSoundPath)
        {
            try
            {
                if (left < -10000 || top < -10000)
                {
                    var current = LoadSettings();
                    if (!double.IsNaN(current.Left)) left = current.Left;
                    if (!double.IsNaN(current.Top)) top = current.Top;
                }

                var settings = new 
                { 
                    Left = left, 
                    Top = top, 
                    IsMini = isMini,
                    HalfTimeSoundPath = halfTimeSoundPath,
                    EndTimeSoundPath = endTimeSoundPath
                };

                File.WriteAllText(_path, JsonConvert.SerializeObject(settings, Formatting.Indented));
            }
            catch { }
        }
    }
}